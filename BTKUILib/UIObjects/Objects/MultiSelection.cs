using System;

namespace BTKUILib.UIObjects.Objects
{
    /// <summary>
    /// This object contains all information and setup for multiselections
    /// </summary>
    public class MultiSelection
    {
        /// <summary>
        /// Option array
        /// </summary>
        public string[] Options;

        /// <summary>
        /// Action to listen for changes of the selection
        /// </summary>
        public Action<int> OnOptionUpdated;

        /// <summary>
        /// Name of the multiselection
        /// </summary>
        public string Name;

        /// <summary>
        /// Get or set the currently selected index
        /// </summary>
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

        /// <summary>
        /// Create a new multiselection object
        /// </summary>
        /// <param name="name">Name to be displayed on the multiselection page when opened</param>
        /// <param name="options">Options to be displayed</param>
        /// <param name="selectedOption">Index of currently selected object</param>
        public MultiSelection(string name, string[] options, int selectedOption)
        {
            Name = name;
            Options = options;
            _selectedOption = selectedOption;
        }
    }
}