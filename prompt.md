Kérlek, készítsd egy single user Time Tracking alkalmazást! 
Ezt tudja: 
 - Az applikáció elindításakor 2 dolog történik: a mögöttes adatfájl (létrejön, ha nincs), betöltődik és normalizálódik 
(ez alatt azt értem, hogy a kézzel írt sorok whitespace és egy hibái kijavítódnak), illetve megjelenik egy ablak, amin a
 user kiválaszthatja, milyen taskkal kezd el foglalkozni. A 10 leggyakoribb megnevezés (`tag: leírás`) bilétaként ki van 
emelve, azokra kattintva egyből el is indul az időmérés. De van szöveges mező is, ahová beírható a megnevezés. Legyen 
(inaktív) stop és pause gomb is, amik elindításkor aktiválódnak. Taskot indítani bármikor lehet, de másik taskot indítva 
az addig aktív leáll. A pause csak arra szolgál, hogy a legutolsót ne kelljen újra beírni/kikeresni.
 - Az időmérés indításakor a betöltött adatfájlt és az új sor kezdetét újra kiírja a mögöttes adatfájlba.
 - a mögöttes adatfájl egy szimpla szövegfájl, amit kézzel is lehet szerkeszteni ilyen struktúrával:
```
Kezdődátum, kezdőidő, vége idő, megnevezés
2026-11-03,08:00:05,08:31:14,Játék:Neuroshima
2026-11-03,16:17:11,17:18:01,Podcast:Premier Leak
```
 - Arra számítok, hogy ez a fájl kézzel is szerkesztve lesz. 
 - Amikor a user leállít egy mérést, akkor újra betöltődik-kiíródik az adatfájl. Ha a leállításkor más nap van, mint 
kezdetben, akkor minden napra új sort hoz létre. Újra megjelenik a választóablak.
 - Ez egy windowsos applikáció lenne, ami a gép bekapcsolásakor elindulna automatikusan. Az időmérés elindulásakor az 
ablak egyből eltűnne, és csak a tray-ben lenne elérhető. A leállítás, szünet, újraindítás, ablak előhozás és kilépés 
elérhető lenne a tray-re kattintva. Erőltetett kilépés (pl. a gép kikapcsolása esetén) állítsa le a futó taskot.
 - A telepítés és futtatás legyen minél kevesebb függőséggel rendelkező. A legjobb az lenne, ha lenne egy telepítő exe,
ami feltelepíti a programot, beteszi a start menübe és a bekapcsoláskor elindítandó programok közé.
 - Van még egy fájl, ami az adatfájl mellett frissül minden állapotváltozáskor: egy html, ami egy napi, heti és havi 
összesítést csinál. A csoportosítás szempontja a tag vagy a tag:leírás (ezt az oldal tetején egy kapcsolóval lehet 
állítani). Az összesítés egy halmozott oszlopdiagram, melynek az utolsó 7 eleme látszik, de balra görgetéssel visszább is
 lehet menni.
 - Az adatfájl és a html a user saját mappájában egy könnyen elérhető helyen legyen. Ezek a fájlok ne legyenek normálisan
lockolva. Ha a program nem fér hozzá valamelyik fájlhoz, jöjjön egy figyelmeztető ablak, amíg a user nem oldja meg a 
helyzetet.