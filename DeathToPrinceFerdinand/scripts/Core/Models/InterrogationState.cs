using System;
using System.Collections.Generic;

namespace DeathToPrinceFerdinand.scripts.Core.Models
{
    public class InterrogationState
    {
        public string SuspectId { get; set; } = string.Empty;
        public List<string> AvailableTestimonyIds { get; set; } = new();
        public int CurrentTestimonyIndex { get; set; } = 0;
        public List<string> RevealedTestimonyIds { get; set; } = new();
        public bool IsComplete { get; set; } = false;

        public bool HasMoreTestimony => CurrentTestimonyIndex < AvailableTestimonyIds.Count;

        public string? GetCurrentTestimonyId()
        {
            if (CurrentTestimonyIndex >= 0 && CurrentTestimonyIndex < AvailableTestimonyIds.Count)
            {
                return AvailableTestimonyIds[CurrentTestimonyIndex];
            }

            return null;
        }

        public void AdvanceToNextTestimony()
        {
            if (HasMoreTestimony)
            {
                var currentId = GetCurrentTestimonyId();
                if (currentId != null && !RevealedTestimonyIds.Contains(currentId))
                {
                    RevealedTestimonyIds.Add(currentId);
                }
                CurrentTestimonyIndex++;

                if (!HasMoreTestimony)
                {
                    IsComplete = true;
                }
            }
        }

        public void UnlockTestimony(string testimonyId)
        {
            if (!AvailableTestimonyIds.Contains(testimonyId))
            {
                AvailableTestimonyIds.Add(testimonyId);
            }
        }
    }
}
