using UnityEngine;

public class GameContext : Singleton<GameContext>
{
    private DirectorBase currentDirector;

    public DirectorBase CurrentDirector
    {
        get => currentDirector;
        set
        {
            if (value != currentDirector)
            {
                var oldDirName = currentDirector ? currentDirector.GetType().ToString() : "null";
                Debug.Log($"local director has changed - old dir : {oldDirName} new dir : {value.GetType()}");
                    
                OnDirectorChanged?.Invoke(currentDirector, value);
            }
               
            currentDirector = value;
        }
    }
    
    public delegate void OnLocalDirectorChanged(DirectorBase previousValue, DirectorBase newValue);
        
    public OnLocalDirectorChanged OnDirectorChanged;
}
