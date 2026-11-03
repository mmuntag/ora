using Ora.Core;

var directory = Path.Combine(Path.GetTempPath(), "ora-tests-" + Guid.NewGuid());
Directory.CreateDirectory(directory);
int checks = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new Exception(description);
    checks++;
    Console.WriteLine("PASS " + description);
}
try
{
    var store = new Store(directory);
    Check(store.Read().Count == 0, "Missing file initializes empty");
    store.Write([]);
    Check(File.Exists(store.DataPath) && File.Exists(store.ReportPath), "Initial outputs created");
    File.WriteAllText(store.DataPath, "Kezdődátum, kezdőidő, vége idő, megnevezés\n 2026-11-3 , 8:00 , 8:31:14 , Játék : Neuroshima \n2026-11-03,16:17:11,17:18:01,\"Podcast:Premier, Leak\"\n");
    var rows = store.Read();
    Check(rows.Count == 2 && rows[0].Name == "Játék: Neuroshima" && rows[1].Name == "Podcast: Premier, Leak", "Manual whitespace, short times, Unicode and quoted commas");
    store.Write(rows);
    Check(store.Read().SequenceEqual(rows), "Canonical CSV roundtrip");
    Check(File.Exists(store.DataPath + ".bak"), "Manual original backed up");
    var split = Entry.Split(new(2026, 11, 3, 23, 15, 0), new(2026, 11, 5, 1, 30, 0), "Munka: Teszt").ToList();
    Check(split.Count == 3 && split.Sum(e => (e.End!.Value - e.Start).TotalMinutes) == 1575, "Multi-day split preserves exact duration");
    Check(split[0].Fields()[2] == "24:00:00", "Midnight serialized without losing a second");
    store.Write(split);
    Check(store.Read().SequenceEqual(split), "Midnight roundtrip");
    Check(Entry.Split(new(2026, 11, 3, 23, 0, 0), new(2026, 11, 4), "Teszt").Count() == 1, "Exact midnight has no phantom empty day");
    File.WriteAllText(store.DataPath, "2026-11-03,23:00,01:00,Teszt\n");
    Check(store.Read().Count == 2, "Manually entered overnight interval split");
    var open = new Entry(new(2026, 11, 3, 8, 0, 0), null, "Munka: </script><script>alert(1)</script>");
    store.Write([open]);
    Check(store.Read().Single().End is null, "Open measurement roundtrip");
    Check(!File.ReadAllText(store.ReportPath).Contains("</script><script>alert"), "Embedded report data escapes HTML script injection");
    store.Checkpoint(open, new(2026, 11, 3, 8, 1, 0));
    Check(store.ReadCheckpoint()?.Start == open.Start, "Recovery checkpoint roundtrip");
    File.WriteAllText(store.DataPath, "2026-11-03,garbage,09:00,Task\n");
    string before = File.ReadAllText(store.DataPath);
    bool failed = false;
    try { store.Read(); } catch (FormatException) { failed = true; }
    Check(failed && File.ReadAllText(store.DataPath) == before, "Malformed input retained without silent data loss");
    File.WriteAllText(store.DataPath, "2026-11-03,08:00,09:00,\"quoted \"\"text\"\"\"\n");
    Check(store.Read()[0].Name == "quoted \"text\"", "Escaped CSV quote parsing");
    File.WriteAllText(store.DataPath, "2026-11-03,08:00,09:00,\"unclosed\n");
    failed = false;
    try { store.Read(); } catch (FormatException) { failed = true; }
    Check(failed, "Broken CSV quoting produces a recoverable validation error");
    failed = false;
    try { Entry.Split(new(2026, 11, 3, 9, 0, 0), new(2026, 11, 3, 8, 0, 0), "Teszt").ToList(); }
    catch (FormatException) { failed = true; }
    Check(failed, "Backward clock rejected instead of recording negative time");
    Console.WriteLine($"{checks} checks passed.");
}
finally { Directory.Delete(directory, true); }
