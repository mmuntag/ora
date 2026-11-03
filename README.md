# Óra

Egyfelhasználós, magyar nyelvű Windows időmérő, .NET 10 és Windows Forms alapon. Futás közben nem igényel internetet, adatbázist vagy külön telepített .NET-et az önálló kiadás használatakor.

## Telepítés és indítás

A GitHub Actions **Windows build** munkafolyamat `Ora-Windows-x64` artifactja tartalmazza a hordozható `Ora.exe` programot és az `Ora-Setup.exe` telepítőt. A munkafolyamat push, pull request és kézi indítás esetén fut. A telepítő felhasználói jogosultsággal telepít, Start menüs parancsikont készít, és alapértelmezetten beállítja a bejelentkezéskori indítást. Ez telepítéskor kikapcsolható. Eltávolításkor a saját adatok megmaradnak.

Forrásból Windows 10/11 x64 rendszeren, .NET 10 SDK-val:

```powershell
dotnet run --project src/Ora
```

Önálló exe készítése:

```powershell
./build.ps1
```

Telepítő készítése (az Inno Setup 6 is szükséges a fejlesztői gépen):

```powershell
./build.ps1 -Installer
```

Kimenet: `artifacts/publish/Ora.exe`, illetve `artifacts/installer/Ora-Setup.exe`. A kiadás a .NET futtatókörnyezetet is tartalmazza. A GitHub windows-latest futtatóján az Inno Setup rendelkezésre áll.

## Használat

Induláskor válassz egy gyakori tevékenységet, vagy írj be egy megnevezést, például `Munka: Tervezés`. A kettőspont nélküli nevek is megengedettek. A tíz bilétát a fájlban található megnevezések gyakorisága határozza meg, egyezésnél a frissebb az első. Indításkor az ablak eltűnik. Új tevékenység indítása lezárja az előzőt. Az aktuális tevékenység neve és az eltelt idő az ablak tetején kiemelve látható. Két vezérlőgomb van: ugyanazon a helyen ▶ (indítás/folytatás), futó mérésnél ⏸ (szünet), mellette pedig mindig ■ (leállítás). Az Enter a play/pause gombot működteti. A leállítás lezárja az aktuális mérést, ha van, és mindig törli a beviteli mezőt és a szünetben megjegyzett nevet is. A gombok fölé mutatva megjelenik a funkció neve. A Kimutatás és az Adatmappa kisebb gombként az ablak alján érhető el.

A tálcaikon bal vagy jobb kattintására megjelenik a menü: ablak/új tevékenység, leállítás, szünet, folytatás, kilépés. Dupla kattintásra megjelenik az ablak. Az ablak X gombja visszarejti az alkalmazást a tálcára. Kilépni a tálcamenüből lehet. A szünet lezárja az aktuális időszakot, a folytatás új időszakot indít; a szünet ideje nem számít bele. Az ablak ▶ gombja a beviteli mezőben lévő névvel indít (ez szünet után is szerkeszthető), a tálcamenü Folytatás pontja a szüneteltetett tevékenység nevével.

## Adatok és kézi szerkesztés

Az adatok helye: **`%USERPROFILE%\Ora`**, például `C:\Users\Anna\Ora`. Az ablak alján lévő Adatmappa gomb megnyitja ezt.

- `idomeres.csv`: UTF-8 szöveg, az első sor fejléc.
- `idomeres.csv.bak`: a legutóbbi átírás előtti fájl.
- `kimutatas.html`: önálló, internet nélkül megnyitható kimutatás.
- `folyamat.json`: a futó mérés helyreállítási életjele, 15 másodpercenként mentve.

```csv
Kezdődátum,kezdőidő,vége idő,megnevezés
2026-11-03,08:00:05,08:31:14,Játék: Neuroshima
2026-11-03,16:17:11,17:18:01,Podcast: Premier Leak
2026-11-03,18:00:00,,Munka: Tervezés
```

Az üres végidő folyamatban lévő mérést jelöl. Leállításkor a program kezdőidő alapján keresi meg a nyitott sort, így annak nevét közben is módosíthatod. Ha kézzel lezárod vagy törlöd az aktív sort, azt tiszteletben tartja; a program saját aktív állapotát a Leállítás gombbal zárhatod le. Az aktív sor kezdőidejét ne változtasd meg, és futás közben ne hozz létre további nyitott sorokat.

A fájl nincs tartósan nyitva vagy zárolva. Minden indítás és állapotváltás újraolvassa. A CSV szerkesztése után mentsd és zárd be a szerkesztőt az állapotváltás előtt; egyszerre történő mentésnél az utolsó írás érvényesül. Átmeneti hozzáférési hiba esetén figyelmeztető ablak jelenik meg, amely az OK után újrapróbálja a műveletet. Folyamatban lévő mentés hiba utáni ismétlése ugyanazt az előkészített állapotot menti, ezért eközben csak a fájl zárolását/jogosultságát javítsd.

A normalizálás eltávolítja a felesleges szóközöket, egységesíti a kettőspont körüli formázást, elfogadja a `8:05` alakú időt és az egyjegyű hónapot/napot is. Vesszőt tartalmazó neveknél szabványos CSV-idézőjelezést használ. Érvénytelen dátumot, időt vagy sérült CSV-t nem talál ki és nem dob el: a hibás sor javításáig figyelmeztet. Ha a végidő kisebb a kezdőidőnél, másnapi befejezésként kezeli.

Az éjfélen átívelő mérések naponként külön sort kapnak. A teljes nap végének jelölése **`24:00:00`**, a következő sor `00:00:00`-kor indul; így nincs elveszett másodperc. Az időmérés helyi faliórával, másodperces felbontással dolgozik; kézi óraátállítás és a téli/nyári átállás befolyásolja az időtartamot. Visszafelé állított óra miatti negatív mérést a program javítást kérve elutasít.

## Kimutatás és helyreállítás

A HTML egy halmozott oszlopdiagramot tartalmaz. Felül a csoportosítás mellett váltható a napi, hétfővel kezdődő heti és havi nézet, valamint a tag és a teljes megnevezés szerinti csoportosítás. Kezdetben a legutóbbi hét időszak látszik; a diagram vízszintesen görgethető a korábbiakhoz. A pontos értékek lenyitható táblázatban is elérhetők, az üres időszakok is szerepelnek. A fájl minden állapotváltáskor újragenerálódik; egy már megnyitott böngészőlapon frissíts az új befejezett mérések betöltéséhez. A benne lévő nyitott mérés időtartama percenként frissül.

Normál kilépéskor, Windows-kijelentkezéskor és leállításkor az alkalmazás lezárja a futó mérést. Áramkimaradás vagy Feladatkezelőből végrehajtott kényszerleállítás esetén egy folyamat nem tud utólag menteni. Következő indításkor ezért helyreállítási kérdés jelenik meg: lezárás az utolsó mentett életjelnél, vagy a jelenlegi idővel. Életjel nélküli kézzel megadott nyitott sornál az első választás nulla hosszúságú mérést jelent; a CSV-ben javítható. A szünetben megjegyzett név csak az aktuális programfutás alatt marad meg.

## Ellenőrzés

A külső tesztkönyvtár nélküli tesztprogram Linuxon is futtatható .NET 10 SDK-val:

```sh
dotnet run --project tests/Ora.Core.Tests -c Release
node tests/report.test.cjs
```

Windowsos kézi ellenőrzéshez: első indítás üres mappával; indítás/rejtés; tálcáról taskváltás; szünet/folytatás; leállítás és kilépés; CSV módosítása futó mérés közben; zárolt CSV és HTML feloldása; éjfélen átnyúló minta betöltése; helyreállítás kényszerleállítás után; telepítő, automatikus indítás és eltávolítás. A natív ablak, tálca és Windows-leállítás viselkedését Windows alatt kell ellenőrizni.

A csomagolás a Microsoft [önálló, egyfájlos .NET-kiadásának](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview) megfelelően történik; a leállítás kezelése a Windows [SessionEnding eseményét](https://learn.microsoft.com/en-us/dotnet/api/microsoft.win32.systemevents.sessionending) is figyeli.

A saját programikon szerkeszthető forrása: `src/Ora/Assets/ora.svg`; a többméretű Windows-ikon: `ora.ico` ugyanitt. Az exe, az ablak, a tálcaikon és a telepítő ezt használja; a Start menüs parancsikon az exe ikonját veszi át.
