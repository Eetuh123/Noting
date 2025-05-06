namespace Noting.Services
{
    public class AppState
    {
        public event Action? NotesChanged;

        public void RaiseNotesChanged() => NotesChanged?.Invoke();
    }
}
