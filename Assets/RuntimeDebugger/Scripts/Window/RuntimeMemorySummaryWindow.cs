using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeDebugger
{
    public class RuntimeMemorySummaryWindow : ScrollableDebuggerWindowBase
    {
        private readonly List<Record> m_Records = new List<Record>();
        private readonly Comparison<Record> m_RecordComparer = RecordComparer;
        private DateTime m_SampleTime = DateTime.MinValue;
        private int m_SampleCount = 0;
        private long m_SampleSize = 0L;
        private bool m_HasEstimatedSample = false;

        protected override void OnDrawScrollableWindow()
        {
            GUILayout.Label("<b>Runtime Memory Summary</b>");
            GUILayout.BeginVertical("box");
            {
                if (GUILayout.Button("Take Sample", GUILayout.Height(30f)))
                {
                    TakeSample();
                }

                if (m_SampleTime <= DateTime.MinValue)
                {
                    GUILayout.Label("<b>Please take sample first.</b>");
                }
                else
                {
                    GUILayout.Label(
                        $"<b>{m_SampleCount} Objects ({GetByteLengthString(m_SampleSize, m_HasEstimatedSample)}) obtained at {m_SampleTime.ToLocalTime():yyyy-MM-dd HH:mm:ss}.</b>");

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("<b>Type</b>");
                        GUILayout.Label("<b>Count</b>", GUILayout.Width(120f));
                        GUILayout.Label("<b>Size</b>", GUILayout.Width(120f));
                    }
                    GUILayout.EndHorizontal();

                    for (int i = 0; i < m_Records.Count; i++)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(m_Records[i].Name);
                            GUILayout.Label(m_Records[i].Count.ToString(), GUILayout.Width(120f));
                            GUILayout.Label(GetByteLengthString(m_Records[i].Size, m_Records[i].Estimated),
                                GUILayout.Width(120f));
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }
            GUILayout.EndVertical();
        }

        private void TakeSample()
        {
            m_Records.Clear();
            m_SampleTime = DateTime.UtcNow;
            m_SampleCount = 0;
            m_SampleSize = 0L;
            m_HasEstimatedSample = false;

            UnityEngine.Object[] samples = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
            for (int i = 0; i < samples.Length; i++)
            {
                bool estimated = false;
                long sampleSize = RuntimeMemorySizeUtility.GetRuntimeMemorySize(samples[i], out estimated);
                string name = samples[i].GetType().Name;
                m_SampleCount++;
                m_SampleSize += sampleSize;
                m_HasEstimatedSample |= estimated;

                Record record = null;
                foreach (Record r in m_Records)
                {
                    if (r.Name == name)
                    {
                        record = r;
                        break;
                    }
                }

                if (record == null)
                {
                    record = new Record(name);
                    m_Records.Add(record);
                }

                record.Count++;
                record.Size += sampleSize;
                record.Estimated |= estimated;
            }

            m_Records.Sort(m_RecordComparer);
        }

        private static int RecordComparer(Record a, Record b)
        {
            int result = b.Size.CompareTo(a.Size);
            if (result != 0)
            {
                return result;
            }

            result = a.Count.CompareTo(b.Count);
            if (result != 0)
            {
                return result;
            }

            return a.Name.CompareTo(b.Name);
        }

        private sealed class Record
        {
            private readonly string m_Name;
            private int m_Count;
            private long m_Size;
            private bool m_Estimated;

            public Record(string name)
            {
                m_Name = name;
                m_Count = 0;
                m_Size = 0L;
                m_Estimated = false;
            }

            public string Name
            {
                get { return m_Name; }
            }

            public int Count
            {
                get { return m_Count; }
                set { m_Count = value; }
            }

            public long Size
            {
                get { return m_Size; }
                set { m_Size = value; }
            }

            public bool Estimated
            {
                get { return m_Estimated; }
                set { m_Estimated = value; }
            }
        }
    }
}