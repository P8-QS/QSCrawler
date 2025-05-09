using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

public class StateManager : MonoBehaviour
{
    public static StateManager Instance;
    public bool promptForEmail = true;
    string _email = "";

    public void SetEmail(string email)
    {
        this._email = email;
        promptForEmail = false;
    }

    private void Awake()
    {
        if (StateManager.Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SceneManager.sceneLoaded += LoadState;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveState()
    {
        Debug.Log("Saving game state...");

        var perks = new List<Perk>();
        foreach (var perk in PerksManager.Instance.Perks)
        {
            perks.Add(new Perk
            {
                name = perk.Value.Name,
                cost = perk.Value.Cost,
                level = perk.Value.Level,
            });
        }

        var state = new State
        {
            experience = ExperienceManager.Instance.Experience,
            perks = perks,
            points = PerksManager.Instance.Points,
        };

        state.email = _email;
        state.promptForEmail = promptForEmail;

        string stateString = JsonUtility.ToJson(state);
        PlayerPrefs.SetString("SaveState", stateString);
    }

    public void LoadState(Scene s, LoadSceneMode mode)
    {
        if (!PlayerPrefs.HasKey("SaveState"))
        {
            promptForEmail = true;
            return;
        }

        var stateString = PlayerPrefs.GetString("SaveState");
        var state = JsonUtility.FromJson<State>(stateString);

        ExperienceManager.Instance.Experience = state.experience;
        PerksManager.Instance.Points = state.points;

        // Set email if available
        if (!string.IsNullOrEmpty(state.email))
        {
            _email = state.email;
            promptForEmail = false;
        }

        // Set perks
        foreach (var perk in state.perks)
        {
            if (PerksManager.Instance.Perks.ContainsKey(perk.name))
            {
                PerksManager.Instance.Perks[perk.name].Level = perk.level;
                PerksManager.Instance.Perks[perk.name].Cost = perk.cost;
            }
            else
            {
                Debug.Log($"Perk {perk.name} not found in the current perks list.");
            }
        }


        Debug.Log("Game state loaded!");
    }
}