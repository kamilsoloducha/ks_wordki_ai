Aplikacja typu fiszki do nauki. Nauka polega na zgadywaniu jednej strony na podstawie drugiej - niekoniecznie musi to być pytanie odpowiedź. czasami sama odpowiedź moze byc pytaniem. 

Backend 3 moduły
1. Users - moduł do logowania, rejestracji uzytkownika
2. Cards - moduł do budowania grup kart, kart, do przeglądania swoich kart i progresu
3. Lessons - moduł do lekcji


moduł Users:
model bazodanowy powinien zawierac podstawowe informacje o uzytkowniku, uzytkownik moze mieć role - aktualnie tylko dwie role User albo Admin. Admin powinien móc się logować jako inny uzytkownik.

moduł Cards:
model:
- user (tworzony przy tworzeniu uzytkwonika w module Users)
- group - moze byc konkretnego uzytkwonika albo statyczna. jezeli grupa jest konkretego uzytkownika to uzytkwonik ma prawo do zmiany grupy i kart wewnatrz grupy.
- card - nalezy do konkretnej grupy. ma dwie strony (Front, Back). kazda ze stron zawiera Label, Example, oraz Comment.
- result - wynik nauki - kazda ze stron moze byc pytaniem a druga strona wtedy jest odpowiedzią. kazda ze stron ma swój wynik - Drawer (poziom w którym znajduje się strona karty), NextRepeat - data kiedy karta ma być przekazana do nauki, Counter - licznik powtórzeń
- user ma miec liste swoich grup


moduł Lessons:
przechowywanie informacji na temat lekcji


technologie:
backend:
- dotnet core, modular monolith, ef core + migrations, clean architecture, mediator, outbox. JWT