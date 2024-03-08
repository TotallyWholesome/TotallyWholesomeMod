using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.UI;
using cohtml.Net;

namespace BTKUILib
{
    /// <summary>
    /// Basic utilities used within the UI
    /// </summary>
    public static class UIUtils
    {
        private static MD5 _hasher = MD5.Create();
        private static FieldInfo _internalCohtmlView = typeof(CohtmlControlledViewWrapper).GetField("_view", BindingFlags.Instance | BindingFlags.NonPublic);
        private static View _internalViewCache;

        /// <summary>
        /// Check if the CVR_MenuManager view is ready
        /// </summary>
        /// <returns>True if view is ready, false if it's not</returns>
        public static bool IsQMReady()
        {
            if (CVR_MenuManager.Instance == null)
                return false;

            return UserInterface.BTKUIReady;
        }

        /// <summary>
        /// Clean non alphanumeric characters from a given string
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns>Cleaned string</returns>
        public static string GetCleanString(string input)
        {
            return Regex.Replace(Regex.Replace(input, "<.*?>", string.Empty), @"[^0-9a-zA-Z_]+", string.Empty);
        }
        
        internal static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            return CreateMD5(inputBytes);
        }

        internal static string CreateMD5(byte[] bytes)
        {
            byte[] hashBytes = _hasher.ComputeHash(bytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }

            return sb.ToString();
        }

        internal static View GetInternalView()
        {
            if (CVR_MenuManager.Instance == null || CVR_MenuManager.Instance.quickMenu == null) return null;

            if (_internalViewCache == null)
                _internalViewCache = (View)_internalCohtmlView.GetValue(CVR_MenuManager.Instance.quickMenu.View);

            return _internalViewCache;
        }
    }
}