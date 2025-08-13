using StuAuthMobile.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace StuAuthMobile.Classe
{
    public class Loc : INotifyPropertyChanged
    {
        public static event EventHandler? LanguageChanged;

        private readonly ResourceManager _resourceManager = Resources.ResourceManager;
        private CultureInfo _culture = CultureInfo.CurrentUICulture;

        public CultureInfo Culture
        {
            get => _culture;
            set
            {
                if (_culture != value)
                {
                    _culture = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                }
            }
        }

        public string this[string key] => _resourceManager.GetString(key, _culture) ?? $"[{key}]";
        public string this[string key, params object[] args]
        {
            get
            {
                string format = _resourceManager.GetString(key, _culture) ?? $"[{key}]";
                return string.Format(format, args);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
