using System;
using System.Collections;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using BepInEx.Configuration;
using UnityEngine;
using System.Reflection;
//using PlasmaLibrary;
namespace PlasmaDeviceFollower {
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    [BepInProcess("Plasma")]
    [BepInDependency(ConfigurationManager.ConfigurationManager.GUID, BepInDependency.DependencyFlags.HardDependency)]
    
    
    public class PlasmaDeviceFollower : BaseUnityPlugin {
        
        
        public const string pluginGuid = "org.bepinex.plugins.PlasmaDeviceFollower";
        public const string pluginName = "Plasma Device Follower";
        public const string pluginVersion = "1.0.0";
        
        public static ConfigEntry<bool> mEnabled;
        public static ConfigEntry<BepInEx.Configuration.KeyboardShortcut> followDevice;
        
        public ConfigDefinition mEnabledDef = new ConfigDefinition(pluginVersion, "Enable/Disable Mod");
        public ConfigDefinition followDeviceDef = new ConfigDefinition(pluginVersion, "Toggle Follow Device");
        
        
        
        public static bool followDeviceJustPressed = false;
        public static bool followDeviceDown = false;
        public static Device deviceToFollow;
        public static bool followingDevice = false;
        public static RigidbodyCharacter rigidbodyCharacter;
        public static WorldController worldController;
        public static PlasmaDeviceFollower instance;
        public PlasmaDeviceFollower(){
            Logger.LogInfo("Activated Device Follower Mod");
            mEnabled = Config.Bind(mEnabledDef, false, new ConfigDescription("Controls if the mod should be enabled or disabled", null, new ConfigurationManagerAttributes {Order = 0}));
            followDevice = Config.Bind(followDeviceDef, new BepInEx.Configuration.KeyboardShortcut(UnityEngine.KeyCode.J), new ConfigDescription("Toggles if the camera should follow a device", null, new ConfigurationManagerAttributes {Order = -1}));
        }
        void Awake(){
            Logger.LogInfo("Device Follower Awake");
            instance = this;
            Harmony.CreateAndPatchAll(typeof(PlasmaDeviceFollower));
        }
        void Update(){
            if(!mEnabled.Value){return;}
            followDeviceJustPressed = followDevice.Value.IsDown();
            if (followDevice.Value.IsDown()){
                followDeviceDown = true;
                if(mEnabled.Value){
                    if(followingDevice){
                        followingDevice = false;
                    }else{
                        try{
                            FieldInfo _targetDevice = typeof(WorldController).GetField("_targetDevice", BindingFlags.NonPublic | BindingFlags.Instance);
                            deviceToFollow = (Device)_targetDevice.GetValue(worldController);
                            if(deviceToFollow != null){
                                followingDevice = true;
                            }
                        }catch (ReflectionTypeLoadException ex){
                            Logger.LogWarning("Couldn't Get _targetDevice");
                        }
                    }
                }
            }
            if (followDevice.Value.IsUp()){
                followDeviceDown = false;
                if(mEnabled.Value){
                    // Code For When Key is Released
                }
            }
            if(followingDevice){
                FieldInfo _localCharacter = typeof(WorldController).GetField("_localCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
                RigidbodyCharacter character = (RigidbodyCharacter)_localCharacter.GetValue(worldController);
                Camera cam = character.camera;
                //Require.UniqueGameObjectWithTag("MainCamera").GetComponent<Camera>().transform.LookAt(deviceToFollow.worldCenter);
                Vector3 direction = deviceToFollow.worldCenter - cam.transform.position;
                Quaternion toRotation = Quaternion.FromToRotation(transform.forward, direction);
                toRotation.eulerAngles = new Vector3(toRotation.eulerAngles.x, toRotation.eulerAngles.y, 0);
                //cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, toRotation, worldController.cameraSmoothing);
                character.UpdateLookAngle(toRotation.eulerAngles.x, toRotation.eulerAngles.y);
                FieldInfo _cameraYaw = typeof(WorldController).GetField("_cameraYaw", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo _cameraPitch = typeof(WorldController).GetField("_cameraPitch", BindingFlags.NonPublic | BindingFlags.Instance);
                _cameraYaw.SetValue(worldController, toRotation.eulerAngles.y);
                _cameraPitch.SetValue(worldController, toRotation.eulerAngles.x);
                //_localCharacter.SetValue(worldController, character);
                //EnvironmentController.gameCamera.transform.LookAt(deviceToFollow.worldCenter);
            }
        }
        
        [HarmonyPatch(typeof(WorldController), "Update")]
        [HarmonyPostfix]
        private static void WorldControllerUpdatePostfixPatch(ref WorldController __instance){
                worldController = __instance;// Write code for patch here
                
           
        }
        
        [HarmonyPatch(typeof(QFSW.QC.QuantumConsole), "Initialize")]
        [HarmonyPrefix]
        private static bool QuantumConsoleInitializePrefixPatch(){
            return false;
        }
        
        [HarmonyPatch(typeof(RigidbodyCharacter), "Awake")]
        [HarmonyPrefix]
        private static bool RigidbodyCharacterAwakePrefixPatch(ref RigidbodyCharacter __instance){
            rigidbodyCharacter = __instance;
            return true;
        }
        
        [HarmonyPatch(typeof(WorldController), "UpdateCameraMouseLook")]
        [HarmonyPrefix]
        private static bool WorldControllerUpdateCameraMouseLookPrefixPatch(){
            
            if(followingDevice && mEnabled.Value){
                // Write code for patch here
                return false; // Cancels Original Function
            }
            return true;
        }
    }
}