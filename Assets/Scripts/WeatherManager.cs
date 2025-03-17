using UnityEngine;
using System.Collections.Generic;
using TMPro; // Import TextMeshPro namespace
using System.Linq;

public class WeatherManager : MonoBehaviour
{
    private Dictionary<string, Dictionary<string, float>> transitionMatrix;
    private string currentWeather;
    private List<string> pastWeatherHistory = new List<string>();

    private int historyLimit = 30; // Store past 30 days of weather
    private int displayLimit = 7;  // Display only last 7 days

    public TextMeshProUGUI weatherHistoryText; // UI Text Reference
    public TextMeshProUGUI predictedWeatherText; // UI for next predicted weather

    public TextMeshProUGUI playerGuessText;  // UI for Player's Guess
    public TextMeshProUGUI guessResultText;  // UI for displaying result

    private string playerGuess;  // Stores player's choice


    void Start()
    {
        InitializeMarkovChain();
        currentWeather = "Sunny"; // Default starting weather

        // Pre-fill history with 30 days of random weather
        PreFillWeatherHistory();

        // Update UI at start
        UpdateWeatherUI();
    }

    void Update()
    {
        // Press SPACEBAR to generate new weather
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateWeather();
        }
    }

    // Initializes the static Markov Chain transition matrix
    void InitializeMarkovChain()
    {
        transitionMatrix = new Dictionary<string, Dictionary<string, float>>()
        {
            { "Sunny", new Dictionary<string, float> { { "Sunny", 0.7f }, { "Cloudy", 0.2f }, { "Rainy", 0.1f } } },
            { "Cloudy", new Dictionary<string, float> { { "Sunny", 0.3f }, { "Cloudy", 0.5f }, { "Rainy", 0.2f } } },
            { "Rainy", new Dictionary<string, float> { { "Sunny", 0.2f }, { "Cloudy", 0.3f }, { "Rainy", 0.5f } } }
        };
    }

    // Generates weather based on the Markov Chain
    public void GenerateWeather()
    {
        // Update probabilities dynamically based on past weather trends
        UpdateTransitionMatrix();

        float rand = Random.value;
        float cumulative = 0f;

        foreach (var nextState in transitionMatrix[currentWeather])
        {
            cumulative += nextState.Value;
            if (rand <= cumulative)
            {
                currentWeather = nextState.Key;
                StoreWeatherHistory(currentWeather);
                Debug.Log("New Weather: " + currentWeather);

                // Update UI
                UpdateWeatherUI();
                PredictNextWeather(); // Show next day's prediction
                return;
            }
        }
    }

    // Analyzes past 7 days to dynamically adjust Markov probabilities
    // Updates the transition matrix based on the last 7 days
    public void UpdateTransitionMatrix()
    {
        if (pastWeatherHistory.Count < displayLimit) return;

        // Count occurrences of each weather type in the last 7 days
        Dictionary<string, int> weatherCount = new Dictionary<string, int>
    {
        { "Sunny", 0 },
        { "Cloudy", 0 },
        { "Rainy", 0 }
    };

        for (int i = pastWeatherHistory.Count - displayLimit; i < pastWeatherHistory.Count; i++)
        {
            weatherCount[pastWeatherHistory[i]]++;
        }

        // Adjust transition probabilities dynamically
        foreach (var weather in transitionMatrix.Keys)
        {
            float total = weatherCount.Values.Sum();

            foreach (var nextState in transitionMatrix[weather].Keys.ToList())
            {
                // Set new probability based on history (normalized)
                transitionMatrix[weather][nextState] = (weatherCount[nextState] + 1f) / (total + 3f);
            }
        }
    }


    // Predicts the next day's weather based on updated probabilities

    // Predicts the next day's weather using Markov Decision Process with randomness
    public void PredictNextWeather()
    {
        if (predictedWeatherText == null) return;

        Dictionary<string, float> expectedUtilities = new Dictionary<string, float>
    {
        { "Sunny", 0f },
        { "Cloudy", 0f },
        { "Rainy", 0f }
    };

        // Compute expected utility for each weather state
        foreach (var weather in transitionMatrix.Keys)
        {
            foreach (var nextState in transitionMatrix[weather])
            {
                // Expected Utility = Transition Probability * Utility (approximated with occurrence count)
                expectedUtilities[nextState.Key] += nextState.Value * GetWeatherUtility(nextState.Key);
            }
        }

        // Normalize utilities into probabilities
        float totalUtility = expectedUtilities.Values.Sum();
        Dictionary<string, float> probabilityDistribution = new Dictionary<string, float>();

        foreach (var state in expectedUtilities)
        {
            probabilityDistribution[state.Key] = (totalUtility > 0) ? state.Value / totalUtility : 1.0f / expectedUtilities.Count;
        }

        // Select next weather based on probability distribution
        string predictedWeather = ChooseWeatherByProbability(probabilityDistribution);
        predictedWeatherText.text = "Predicted Next Day: " + predictedWeather;
    }

    // Chooses a weather state using weighted random selection
    private string ChooseWeatherByProbability(Dictionary<string, float> probabilities)
    {
        float rand = UnityEngine.Random.value; // Random number between 0 and 1
        float cumulative = 0f;

        foreach (var state in probabilities)
        {
            cumulative += state.Value;
            if (rand <= cumulative) return state.Key;
        }

        return probabilities.Keys.First(); // Fallback
    }

    // Assigns utility values based on past occurrences in the last 7 days
    public float GetWeatherUtility(string weather)
    {
        if (pastWeatherHistory.Count < displayLimit) return 1.0f; // Default utility

        int count = 0;
        for (int i = pastWeatherHistory.Count - displayLimit; i < pastWeatherHistory.Count; i++)
        {
            if (pastWeatherHistory[i] == weather) count++;
        }

        // More occurrences = higher utility, but avoids bias by adding a small random factor
        return ((float)count / displayLimit) + UnityEngine.Random.Range(0.01f, 0.05f);
    }





    // Stores weather in history and maintains a max of 30 days
    private void StoreWeatherHistory(string newWeather)
    {
        pastWeatherHistory.Add(newWeather);

        if (pastWeatherHistory.Count > historyLimit)
        {
            pastWeatherHistory.RemoveAt(0); // Remove oldest entry
        }
    }

    // Updates the UI with the last 7 days of weather (Horizontal Icons)
    // Updates the UI with weather history using words instead of icons
    private void UpdateWeatherUI()
    {
        if (weatherHistoryText == null)
        {
            Debug.LogError("WeatherHistoryText UI is not assigned in the Inspector!");
            return;
        }

        // Get only the last 7 days
        int start = Mathf.Max(0, pastWeatherHistory.Count - displayLimit);
        List<string> recentWeather = pastWeatherHistory.GetRange(start, pastWeatherHistory.Count - start);

        // Display the weather words in a single line
        weatherHistoryText.text = "Last 7 Days: " + string.Join(" | ", recentWeather);
    }


    // Pre-fills history with 30 days of weather before game starts
    private void PreFillWeatherHistory()
    {
        for (int i = 0; i < historyLimit; i++)
        {
            currentWeather = GenerateRandomWeather();
            pastWeatherHistory.Add(currentWeather);
        }
    }

    // Generates a random weather state without affecting history
    private string GenerateRandomWeather()
    {
        float rand = Random.value;
        float cumulative = 0f;

        foreach (var nextState in transitionMatrix[currentWeather])
        {
            cumulative += nextState.Value;
            if (rand <= cumulative)
            {
                return nextState.Key;
            }
        }
        return currentWeather;
    }

    // Generates new weather based on transition matrix and updates it
    public void GenerateNewWeather()
    {
        UpdateTransitionMatrix();  // Update Markov model

        currentWeather = ChooseWeatherByProbability(transitionMatrix[currentWeather]);

        // Add new weather and remove old ones
        pastWeatherHistory.Add(currentWeather);
        if (pastWeatherHistory.Count > 30) pastWeatherHistory.RemoveAt(0);

        UpdateWeatherUI();
        PredictNextWeather();

        CheckPlayerGuess(); // Check player's guess after generating weather
    }


    // Player selects a weather guess
    public void PlayerGuessesWeather(string guessedWeather)
    {
        playerGuess = guessedWeather;
        playerGuessText.text = "Your Guess: " + playerGuess;
    }

    // Compare Player's Guess to the actual generated weather
    public void CheckPlayerGuess()
    {
        if (string.IsNullOrEmpty(playerGuess)) return; // Ensure a guess was made

        if (playerGuess == currentWeather)
        {
            guessResultText.text = "Correct! The weather is " + currentWeather;
        }
        else
        {
            guessResultText.text = "Wrong! The weather is " + currentWeather;
        }
    }

}
