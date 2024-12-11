namespace Rise.Client.Services
{
    public class ProfileStateService
    {
        public string AvatarBase64 { get; private set; }

        public void UpdateAvatar(string avatarBase64)
        {
            AvatarBase64 = avatarBase64;
            NotifyStateChanged(); // Notify the state has changed
        }
        public event Action OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}