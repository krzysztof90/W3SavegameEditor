using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace W3SavegameEditor.Core.Common
{
    public class ReadSavegameProgress : IReadSavegameProgress, INotifyPropertyChanged
    {
        private bool _running;
        private bool _indeterministic;
        private int _value;
        private int _max;

        public bool Running
        {
            get => _running;
            set
            {
                if (_running != value)
                {
                    _running = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Indeterministic
        {
            get => _indeterministic;
            set
            {
                if (_indeterministic != value)
                {
                    _indeterministic = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Max
        {
            get => _max;
            set
            {
                if (_max != value)
                {
                    _max = value;
                    OnPropertyChanged();
                }
            }
        }

        public void Report(bool running, bool indeterministic, int value, int max)
        {
            Running = running;
            Indeterministic = indeterministic;
            Value = value;
            Max = max;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
