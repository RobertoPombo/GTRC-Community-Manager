using System;
using System.Collections.ObjectModel;
using System.Data;

namespace GTRCLeagueManager
{
    public class RuleVM : ObservableObject
    {

        public static ObservableCollection<Rule> ruleList = new ObservableCollection<Rule>();
        public static Rule currentRule = null;

        private Rule selectedRule = null;

        public RuleVM()
        {
            RuleList.Add(new Rule());
            SelectedRule = RuleList[0];
            this.AddRule = new UICmd((o) => RuleList.Add(new Rule()));
            this.DelRule = new UICmd((o) => RuleList.Remove((Rule)o));
            this.SelRule = new UICmd((o) => SelectedRule = (Rule)o);
        }

        public ObservableCollection<Rule> RuleList
        {
            get { return ruleList; }
            set
            {
                if (ruleList != value)
                {
                    ruleList = value;
                    RaisePropertyChanged();
                }
            }
        }

        public Rule SelectedRule
        {
            get { return selectedRule; }
            set { selectedRule = value; RaisePropertyChanged(); currentRule = selectedRule; }
        }

        public UICmd AddRule { get; set; }
        public UICmd DelRule { get; set; }
        public UICmd SelRule { get; set; }
    }
}
