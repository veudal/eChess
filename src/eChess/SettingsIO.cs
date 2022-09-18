using eChess.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace eChess
{
    public static class SettingsIO
    {
        static readonly string settingsFilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\eChess\\settings.xml";

        internal static void Load()
        {
            if (!File.Exists(settingsFilePath))
            {
                Settings.Default.PlayerGuid = Guid.NewGuid();
                Settings.Default.Save();
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                config.SaveAs(settingsFilePath);
            }
            else
            {
                var appSettings = Settings.Default;
                try
                {
                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                    string appSettingsXmlName = Properties.Settings.Default.Context["GroupName"].ToString();
                    var import = XDocument.Load(settingsFilePath);
                    var settings = import.XPathSelectElements("//" + appSettingsXmlName);
                    config.GetSectionGroup("userSettings").Sections[appSettingsXmlName].SectionInformation.SetRawXml(settings.Single().ToString());
                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection("userSettings");
                    appSettings.Reload();
                }
                catch
                {
                    appSettings.Reload();
                }
            }
        }

        internal static void Save()
        {
            Settings.Default.Save();
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            config.SaveAs(settingsFilePath);
        }
    }
}
