using UnityEngine;

namespace EVRC.DesktopUI
{
    using VJoyStatus = vJoyInterface.VJoyStatus;

    [RequireComponent(typeof(TMPro.TextMeshProUGUI))]
    public class vJoyStatus : MonoBehaviour
    {
        protected TMPro.TextMeshProUGUI textMesh;

        private void OnEnable()
        {
            textMesh = GetComponent<TMPro.TextMeshProUGUI>();
            vJoyInterface.VJoyStatusChange.Listen(OnStatusChange);
        }

        private void OnDisable()
        {
            vJoyInterface.VJoyStatusChange.Remove(OnStatusChange);
        }

        private void OnStatusChange(vJoyInterface.VJoyStatusChanged status)
        {
            textMesh.text = $"#{status.deviceId}: {GetStatusText(status.status)}";
        }

        private string GetStatusText(VJoyStatus status)
        {
            switch (status)
            {
                case VJoyStatus.NotInstalled:
                    return "Not installed";
                case VJoyStatus.VersionMismatch:
                    return "Incompatible version";
                case VJoyStatus.DeviceUnavailable:
                    return "Device is unavailable";
                case VJoyStatus.DeviceOwned:
                    return "Device in use by other application";
                case VJoyStatus.DeviceError:
                    return "Unknown device error";
                case VJoyStatus.DeviceMisconfigured:
                    return "Device misconfigured";
                case VJoyStatus.DeviceNotAquired:
                    return "Failed to aquire device";
                case VJoyStatus.Ready:
                    return "Connected to device";
                default:
                    return "Unknown";
            }
        }
    }
}
