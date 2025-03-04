using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneController
{
    public static List<int> testScenes;
    private static int sceneCount = 6;
    private static bool ready = false;

    public static void SetupScenes()
    {
        if (testScenes != null) return;
        testScenes = new();

        // Based on "Bradley, J. V. Complete counterbalancing of immediate sequential effects in a Latin square design. J. Amer. Statist. Ass.,.1958, 53, 525-528. "
        // Code from https://damienmasson.com/tools/latin_square/
        
        int j = 0, h = 0, i = 0;

        int[] conditions = Enumerable.Range(1, sceneCount).ToArray();

        int participantId = PlayerPrefs.GetInt("run", 1);

        for (i = 0; i < conditions.Length; ++i)
        {
            var val = 0;
            if (i < 2 || i % 2 != 0)
            {
                val = j++;
            }
            else
            {
                val = conditions.Length - h - 1;
                ++h;
            }

            var idx = (val + participantId) % conditions.Length;
            testScenes.Add(conditions[idx]);
        }

        if (conditions.Length % 2 != 0 && participantId % 2 != 0)
        {
            testScenes.Reverse();
        }
    }

    public static int NextScene(int source)
    {
        SetupScenes();

        if (source == 0) { ready = true; return -1; }

        if (ready == false) return -1;

        using (StreamWriter file = File.AppendText(Path.Combine(Application.persistentDataPath, $"TestOrder_{PlayerPrefs.GetInt("run", 1)}.txt")))
        {
            file.WriteLine(SceneManager.GetActiveScene().name);
        }

        if (testScenes.Count == 0)
        {
            Application.Quit();
            return -1;
        }

        int sceneIndex = Random.Range(0, testScenes.Count);
        int toLoad = testScenes[sceneIndex];
        ready = false;

        testScenes.RemoveAt(sceneIndex);
        return toLoad;
    }

}
