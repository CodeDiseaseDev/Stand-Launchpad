using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StandLaunchpad
{
    public class Settings
    {
        public bool AdvancedMode = true;

        public void Save(string file)
        {
            string str = JsonConvert.SerializeObject(this);
            File.WriteAllText(file, str);
        }

        public static Settings ReadFile(string file)
        {
            if (!File.Exists(file))
                new Settings().Save(file);

            return JsonConvert.DeserializeObject<Settings>(
                File.ReadAllText(file)
            );
        }
    }
}
