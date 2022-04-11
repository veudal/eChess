using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace eChess
{
    internal class TimerModel : INotifyPropertyChanged
    {
        private string _timer;

        public string Timer
        {
            get { return _timer; }
            set
            {
                _timer = value;
                OnPropertyChanged(nameof(Timer));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
