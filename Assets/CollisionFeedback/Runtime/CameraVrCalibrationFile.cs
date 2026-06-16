using System.IO;
using UnityEngine;
using CollisionFeedback.Core;

namespace CollisionFeedback.Runtime
{
    /// <summary>
    /// File I/O for the camera-rig -> VR-frame calibration. Delegates the text format to the pure Core
    /// <see cref="RigidTransformSerializer"/> and owns the single shared on-disk location, so the calibration
    /// writer (<see cref="CameraVrCalibration"/>) and the session reader (LiveSessionController) never diverge.
    /// </summary>
    public static class CameraVrCalibrationFile
    {
        public const string FileName = "cam_to_vr_calib.txt";

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        public static void Save(in RigidTransform t, string path = null)
        {
            File.WriteAllText(string.IsNullOrWhiteSpace(path) ? DefaultPath : path,
                              RigidTransformSerializer.Format(t));
        }

        public static bool TryLoad(out RigidTransform t, string path = null)
        {
            t = RigidTransform.Identity;
            string p = string.IsNullOrWhiteSpace(path) ? DefaultPath : path;
            if (!File.Exists(p)) return false;
            return RigidTransformSerializer.TryParse(File.ReadAllText(p), out t);
        }
    }
}
