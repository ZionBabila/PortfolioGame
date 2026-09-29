using System;
using System.Collections.Generic;
using UnityEngine;

namespace Portfolio
{
    /// <summary>
    /// What the HR recruiter NPC says: an intro for the first meeting, greetings for later ones, and the questions
    /// with answer choices and the recruiter's reply to each. Edit the asset in the Inspector — it's content, not code.
    /// </summary>
    [CreateAssetMenu(menuName = "Portfolio/HR Question Set", fileName = "HRQuestions")]
    public class HRQuestionSet : ScriptableObject
    {
        public string speakerName = "Dana · HR";
        [Tooltip("Said before the first question, the first time the recruiter catches the visitor.")]
        [TextArea] public List<string> intros = new();
        [Tooltip("Said before the question on every later meeting (one at random).")]
        [TextArea] public List<string> greetings = new();
        [Tooltip("Label of the button that closes the dialog after the reply (one at random).")]
        public List<string> goodbyes = new();
        public string runAwayLabel = "Run away!";
        public List<HRQuestion> questions = new();
    }

    [Serializable]
    public class HRQuestion
    {
        [TextArea] public string question;
        public List<HRAnswer> answers = new();
    }

    [Serializable]
    public class HRAnswer
    {
        public string text;
        [TextArea] public string reply;
    }
}
