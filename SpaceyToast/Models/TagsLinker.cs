using SpaceyToast.ViewModels;

namespace SpaceyToast.Models
{
    public class TagsLinker : ViewModelBase
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set 
            { 
                _name = value;
                NotifyPropertyChanged(nameof(Name));
            }
        }

        bool _isAssigned;
        public bool IsAssigned
        {
            get { return _isAssigned; }
            set 
            { 
                _isAssigned = value;
                NotifyPropertyChanged(nameof(IsAssigned));
            }
        }

        public TagsLinker(string name, bool isAssigned)
        {
            Name = name;
            IsAssigned = isAssigned;
        }
    }
}
