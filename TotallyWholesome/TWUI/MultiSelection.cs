using System;

namespace TotallyWholesome.TWUI
{
    public class MultiSelection
    {
        public string[] Options;

        public Action<int> OnOptionUpdated;

        public string Name;

        public int SelectedOption
        {
            get => _selectedOption;
            set
            {
                _selectedOption = value;
                OnOptionUpdated?.Invoke(_selectedOption);
            }
        }

        private int _selectedOption = -1;

        public MultiSelection(string name, string[] options, int selectedOption)
        {
            Name = name;
            Options = options;
            _selectedOption = selectedOption;
        }
    }
}