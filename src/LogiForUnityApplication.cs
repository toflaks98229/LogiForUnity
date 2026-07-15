namespace Loupedeck.LogiForUnityPlugin
{
    using System;

    // This class connects the Loupedeck plugin to the Unity Editor.

    public class LogiForUnityApplication : ClientApplication
    {
        public LogiForUnityApplication()
        {
        }

        // The Unity Editor runs as Unity.exe on Windows.
        protected override String GetProcessName() => "Unity";

        // The Unity Editor bundle on macOS.
        protected override String GetBundleName() => "com.unity3d.UnityEditor5.x";

        // Let the plugin service decide based on the process/bundle name instead of probing the filesystem,
        // because the Unity Hub installs each editor version into its own directory.
        public override ClientApplicationStatus GetApplicationStatus() => ClientApplicationStatus.Unknown;
    }
}
