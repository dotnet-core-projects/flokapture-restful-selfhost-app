using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessLayer.DbEntities
{
    [DebuggerStepThrough]
    public class UniverseFileMenu : EntityBase
    {
        private string _menuTitle;
        private string _menuDescription;

        [Required]
        [BsonRequired]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ProjectId { get; set; }
        [BsonIgnore]
        public string WorkflowMenuName
        {
            get
            {
                if (string.IsNullOrEmpty(MenuDescription))
                    return MenuTitle;

                if (MenuDescription.StartsWith("-") || MenuDescription.StartsWith(" -"))
                    return $"{MenuTitle} {MenuDescription}";

                return $"{MenuTitle} - {MenuDescription}";
            }
        }
        public string MenuId { get; set; }
        public string MenuTitle
        {
            get => _menuTitle;
            set
            {
                if (string.IsNullOrEmpty(value)) return;

                string titleValue = value;
                titleValue = Regex.Replace(titleValue, @"[ ]{2,}", " - ");
                _menuTitle = titleValue;
            }
        }
        public string MenuDescription
        {
            get => _menuDescription;
            set
            {
                if (string.IsNullOrEmpty(value)) return;

                string titleValue = value; //.ToLower();
                var regex = new Regex(@"@\(\d+\,\d+\)", RegexOptions.IgnoreCase);
                if (regex.IsMatch(titleValue))
                {
                    foreach (Group group in regex.Match(titleValue).Groups)
                    {
                        var grpValue = titleValue.Substring(group.Value.Length).Trim();
                        _menuDescription = grpValue; // CultureInfo.CurrentCulture.TextInfo.ToTitleCase(grpValue);
                        break;
                    }
                }
                else
                    _menuDescription = titleValue; // CultureInfo.CurrentCulture.TextInfo.ToTitleCase(titleValue);
            }
        }
        public string ActionExecuted { get; set; }
        public override string ToString()
        {
            return MenuTitle;
        }
    }
}