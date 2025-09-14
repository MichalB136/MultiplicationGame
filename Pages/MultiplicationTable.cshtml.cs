using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace MultiplicationGame.Pages
{

    public class MultiplicationTableModel : PageModel
    {
        [BindProperty]
        public List<int?>? Answers { get; set; }
        public List<string>? Results { get; set; }
        public int CorrectCount { get; set; }
        public List<int> Proposals { get; set; } = new();
        public List<bool?> CellCorrectness { get; set; } = new(); // null - nie sprawdzono, true - poprawne, false - błędne

        public void OnGet()
        {
            Answers = new List<int?>();
            CellCorrectness = new List<bool?>();
            for (int i = 0; i < 100; i++)
            {
                Answers.Add(null);
                CellCorrectness.Add(null);
            }
            GenerateProposals();
        }

        [BindProperty]
        public List<int> ProposalsState { get; set; } = new();

        public void OnPost()
        {
            Results = new List<string>();
            CorrectCount = 0;
            CellCorrectness = new List<bool?>();
            if (Answers == null) return;
            for (int i = 1; i <= 10; i++)
            {
                for (int j = 1; j <= 10; j++)
                {
                    int index = (i - 1) * 10 + (j - 1);
                    int correct = i * j;
                    int? user = Answers.Count > index ? Answers[index] : null;
                    if (user == null)
                    {
                        CellCorrectness.Add(null); // nie sprawdzaj pustych
                        continue;
                    }
                    if (user == correct)
                    {
                        CorrectCount++;
                        CellCorrectness.Add(true);
                    }
                    else
                    {
                        CellCorrectness.Add(false);
                        Results.Add($"{i} × {j} = {correct}, Twoja odpowiedź: {user}");
                    }
                }
            }
            // Propozycje pobieramy z formularza jeśli są, jeśli nie - generujemy
            if (ProposalsState != null && ProposalsState.Count > 0)
            {
                Proposals = new List<int>(ProposalsState);
            }
            else
            {
                GenerateProposals();
            }
            // Jeśli po dropie nie ma już żadnych propozycji, a są jeszcze puste pola, generuj nowe
            if ((Proposals == null || Proposals.Count == 0) && Answers != null && Answers.Exists(a => a == null))
            {
                GenerateProposals();
            }
        }

        private void GenerateProposals()
        {
            var emptyIndexes = new List<int>();
            for (int i = 0; i < 100; i++)
            {
                if (Answers == null || Answers[i] == null)
                    emptyIndexes.Add(i);
            }
            var random = new System.Random();
            var proposals = new HashSet<int>();
            foreach (var idx in emptyIndexes)
            {
                int row = idx / 10 + 1;
                int col = idx % 10 + 1;
                proposals.Add(row * col);
            }
            // Losowo wybierz do 10 propozycji
            Proposals = proposals.OrderBy(x => random.Next()).Take(10).ToList();
        }
    }
}
