using System;

namespace SFS
{
    public class PlayerScores
    {
        static private int[] m_topScores = [0, 0, 0, 0, 0];
        static private TimeSpan[] m_topTimes = [new(0, 0, 0), new(0, 0, 0),
            new(0, 0, 0), new(0, 0 , 0), new(0, 0, 0)];
        static readonly private string kFileName = "scores";
        static readonly private char kSeperator = '/';

        public static int[] GetScores() { return m_topScores; }

        public static TimeSpan[] GetTimes() { return m_topTimes; }

        // Return true if new high score.
        public static bool AddScore(int score)
        {
            // The new score is smaller than the smallest score, so
            // don't push it.
            if (m_topScores[m_topScores.Length - 1] > score)
                return false;

            if (m_topScores[0] < score)
            {
                // Walk back the scores.
                for (int i = m_topScores.Length - 1; i > 0; i--)
                    m_topScores[i] = m_topScores[i - 1];

                // Set new top score.
                m_topScores[0] = score;

                return true;
            }

            for (int i = 0; i < m_topScores.Length; i++)
            {
                if (m_topScores[i] < score)
                {
                    // Walk back the scores.
                    for (int j = m_topScores.Length - 1; j > i; j--)
                        m_topScores[j] = m_topScores[j - 1];

                    // Set score.
                    m_topScores[i] = score;
                    return false;
                }
            }

            return false;
        }

        // Return true if new longest time alive score.
        public static bool AddTime(TimeSpan time)
        {
            // The new score is smaller than the smallest score, so
            // don't push it.
            if (m_topTimes[m_topTimes.Length - 1] > time)
                return false;

            if (m_topTimes[0] < time)
            {
                // Walk back the scores.
                for (int i = m_topTimes.Length - 1; i > 0; i--)
                    m_topTimes[i] = m_topTimes[i - 1];

                // Set new top score.
                m_topTimes[0] = time;

                return true;
            }

            for (int i = 0; i < m_topTimes.Length; i++)
            {
                if (m_topTimes[i] < time)
                {
                    // Walk back the scores.
                    for (int j = m_topTimes.Length - 1; j > i; j--)
                        m_topTimes[j] = m_topTimes[j - 1];

                    // Set score.
                    m_topTimes[i] = time;
                    return false;
                }
            }

            return false;
        }

        public static void Save()
        {
            string stringToSave = "";

            for (int i = 0; i < m_topScores.Length; i++)
            {
                stringToSave += m_topScores[i].ToString();
                stringToSave += kSeperator;
            }

            for (int i = 0; i < m_topTimes.Length; i++)
            {
                stringToSave += m_topTimes[i].ToString();
                stringToSave += kSeperator;
            }

            DataSaver.Save(stringToSave, kFileName);
        }

        public static void Load()
        {
            Godot.Variant data = DataSaver.Load(kFileName);

            string dataString;
            if (!Utilities.VariantIsType(data, out dataString))
            {
                Debug.PrintWarning("Loaded data in PlayerScores is not a string!");
                return;
            }

            dataString = dataString.Remove(0, 3);
            dataString = dataString.Remove(dataString.Length - 1, 1);

            // Read scores
            for (int i = 0; i < m_topScores.Length; i++)
            {
                int index = dataString.IndexOf(kSeperator);
                if (index == -1)
                {
                    Debug.PrintWarning("The top scores part of the loaded string in PlayerScores is not formatted correctly!");
                    return;
                }
                string score = dataString.Substring(0, index);
                m_topScores[i] = int.Parse(score);
                dataString = dataString.Remove(0, index + 1);
            }

            // Read times
            for (int i = 0; i < m_topTimes.Length; i++)
            {
                int index = dataString.IndexOf(kSeperator);
                if (index == -1)
                {
                    Debug.PrintWarning("The top times part of the loaded string in PlayerScores is not formatted correctly!");
                    return;
                }
                string time = dataString.Substring(0, index);
                m_topTimes[i] = TimeSpan.Parse(time);
                dataString = dataString.Remove(0, index + 1);
            }
        }
    }
}
