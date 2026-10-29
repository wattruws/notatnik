# Notatnik

Prosty notatnik desktopowy w Avalonia. Wymaga .NET 10 SDK; projekt jest w
`notatnik/notatnik.csproj`.

## Zapis notatek

Notatki trafiają do pliku `notatki.md` obok pliku projektu. Każdy wpis ma
nagłówek z datą, godziną i tytułem, a treść może zawierać wiele linii:

```text
## 2026-09-08 14:32 — Zakupy
mleko
chleb

## 2026-09-07 09:15 — Pomysł na projekt
minutnik z historią sesji
```

Plik jest zapisywany jako UTF-8. Wpisy są sortowane od najnowszego; przy
błędzie zapisu aplikacja wyświetla powód pod przyciskiem, a lista pozostaje
bez zmian.

## Notatki z ćwiczenia

Repozytorium zawiera początkowy commit `runda 1`, ale w dostępnych plikach nie
ma zapisu dwóch notatek ani wyników uruchomień z rund 1–5. Nie dopisano
zmyślonych zrzutów wyników ani różnic z powtórki. Aby uzupełnić porównanie,
zapisz rzeczywiste pliki z każdego uruchomienia do `rundy/runda-1.md` –
`rundy/runda-5.md` oraz wybrany drugi wynik do
`rundy/runda-X-powtorka.md`. W README należy wtedy zanotować różnice z
powtórki i ocenić, jak niedeterministyczność modelu mogła wpłynąć na rundy
1–4.

## Wskazówka dotycząca promptów

GitHub zaleca dzielenie dużych zadań na mniejsze, jednoznaczne kroki
([Prompt engineering for GitHub Copilot](https://docs.github.com/en/copilot/concepts/prompting-and-context/prompt-engineering),
odczytano 29 października 2026). W przypadku notatnika warto osobno opisać
format pliku, kolejność wpisów i obsługę błędu; ułatwia to sprawdzenie każdej
z tych właściwości zamiast oceny całej aplikacji naraz.
