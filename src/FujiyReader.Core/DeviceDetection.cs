using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;

namespace FujiyReader.Core
{
    public class DeviceDetection
    {
        public static Platform DetectPlatform()
        {
            bool isHardwareButtonsAPIPresent = ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons");

            if (isHardwareButtonsAPIPresent)
            {
                return Platform.WindowsPhone;
            }
            else
            {
                return Platform.Windows;
            }
        }
    }
}
