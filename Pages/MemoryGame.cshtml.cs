using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;

namespace MultiplicationGame.Pages
{
    public class MemoryGameModel : PageModel
    {
        public class Card
        {
            public string Display { get; set; } = ""; // np. "6×4" lub "24"
            public int Value { get; set; } // iloczyn
            public bool IsExpression { get; set; } // true: działanie, false: wynik
        }

        public List<Card> Cards { get; set; } = new();
        public List<bool> Flipped { get; set; } = new();
        public List<bool> Matched { get; set; } = new();

        [BindProperty]
        public List<int> State { get; set; } = new(); // 0: zakryta, 1: odkryta, 2: dopasowana
        [BindProperty]
        public int? FlipIndex { get; set; }


        [BindProperty]
        public List<string> CardDisplays { get; set; } = new();
        [BindProperty]
        public List<int> CardValues { get; set; } = new();
        [BindProperty]
        public List<bool> CardIsExpression { get; set; } = new();

        public void OnGet()
        {
            // Nowa gra: generuj karty i stan
            var pairs = new List<(string expr, int val)>{
                ("2×3",6),("3×4",12),("4×5",20),("5×6",30),("2×7",14),("3×7",21),("4×8",32),("5×9",45)
            };
            var cards = new List<Card>();
            foreach (var (expr, val) in pairs)
            {
                cards.Add(new Card { Display = expr, Value = val, IsExpression = true });
                cards.Add(new Card { Display = val.ToString(), Value = val, IsExpression = false });
            }
            var rnd = new System.Random();
            Cards = cards.OrderBy(x => rnd.Next()).ToList();
            CardDisplays = Cards.Select(c => c.Display).ToList();
            CardValues = Cards.Select(c => c.Value).ToList();
            CardIsExpression = Cards.Select(c => c.IsExpression).ToList();
            State = Enumerable.Repeat(0, Cards.Count).ToList();
            UpdateLists();
        }

        public void OnPost()
        {
            // Odtwórz karty z przesłanych danych
            bool valid = CardDisplays != null && CardValues != null && CardIsExpression != null
                && CardDisplays.Count == CardValues.Count && CardDisplays.Count == CardIsExpression.Count
                && State != null && State.Count == CardDisplays.Count;
            if (!valid)
            {
                // Dane nieprawidłowe, zainicjuj nową grę
                OnGet();
                return;
            }
            Cards = new List<Card>();
            for (int i = 0; i < CardDisplays.Count; i++)
            {
                Cards.Add(new Card { Display = CardDisplays[i], Value = CardValues[i], IsExpression = CardIsExpression[i] });
            }
            if (FlipIndex.HasValue && FlipIndex.Value >= 0 && FlipIndex.Value < State.Count)
            {
                // Odkryj kartę
                if (State[FlipIndex.Value] == 0)
                    State[FlipIndex.Value] = 1;

                // Sprawdź, czy odkryto dwie
                var flipped = State.Select((v, i) => (v, i)).Where(x => x.v == 1).ToList();
                if (flipped.Count == 2)
                {
                    var a = Cards[flipped[0].i];
                    var b = Cards[flipped[1].i];
                    if (a.Value == b.Value && a.IsExpression != b.IsExpression)
                    {
                        State[flipped[0].i] = 2;
                        State[flipped[1].i] = 2;
                    }
                    else
                    {
                        State[flipped[0].i] = 0;
                        State[flipped[1].i] = 0;
                    }
                }
            }
            UpdateLists();
        }

        private void UpdateLists()
        {
            Flipped = State.Select(x => x == 1).ToList();
            Matched = State.Select(x => x == 2).ToList();
        }
    }
}
