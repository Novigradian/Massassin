using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool levelEnded = false;
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called by MaskController when possession changes
    public void OnPossessionChanged(Transform newTarget)
    {
        if (levelEnded) return;

        if (newTarget.CompareTag("TargetEnemy"))
        {
            HandleWin();
        }
    }
    void HandleWin()
    {
        if (levelEnded) return;
        
        levelEnded = true;
        Debug.Log("YOU WIN");

        // Delay optional (for animation / VFX)
        //Invoke(nameof(LoadNextLevel), 1.5f);
    }

    void LoadNextLevel()
    {
        levelEnded = false;

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentIndex + 1);
    }

    public void HandleLose()
    {
        if (levelEnded) return;

        levelEnded = true;
        Debug.Log("YOU LOSE");

        //Invoke(nameof(ReloadLevel), 1.5f);
        Canvas.Instance.ShowLosePanel();
    }

    void ReloadLevel()
    {
        levelEnded = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
