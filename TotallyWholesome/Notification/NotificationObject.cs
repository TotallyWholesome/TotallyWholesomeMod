using UnityEngine;

namespace TotallyWholesome.Notification
{
    public class NotificationObject
    {
        public string Title;
        public string Description;
        public Sprite Icon;
        public float DisplayLength;
        public Color BackgroundColor;
        public bool UseAchievementPopup;

        public NotificationObject(string title, string description, Sprite icon, float displayLength, Color backgroundColor, bool useAchievementPopup = false)
        {
            Title = title;
            Description = description;
            Icon = icon;
            DisplayLength = displayLength;
            BackgroundColor = backgroundColor;
            UseAchievementPopup = useAchievementPopup;
        }
    }
}