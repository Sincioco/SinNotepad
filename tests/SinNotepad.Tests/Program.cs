using System.Text;
using SinNotepad.Core;

string root = Path.Combine(Path.GetTempPath(), "SinNotepadTests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
int passed = 0, failed = 0;
void Test(string name, Action action) { try { action(); Console.WriteLine("PASS " + name); passed++; } catch (Exception ex) { Console.WriteLine("FAIL " + name + ": " + ex.Message); failed++; } }
void Assert(bool condition, string message = "Assertion failed") { if (!condition) throw new Exception(message); }
foreach (string enc in new[] { "UTF-8", "UTF-8 with BOM", "UTF-16 LE", "UTF-16 BE", "UTF-32 LE", "UTF-32 BE" })
    foreach (string ending in new[] { "\r\n", "\n", "\r" })
        Test($"Round-trip {enc} / {ending.Length}:{(int)ending[0]}", () =>
        {
            var doc = new Document { Text = "café 中文 😀\nsecond\n", EncodingName = enc, NewLine = ending };
            string path = Path.Combine(root, Guid.NewGuid() + ".txt"); TextFiles.Save(doc, path);
            var read = TextFiles.Open(path); Assert(read.Text == doc.Text); Assert(read.EncodingName == enc); Assert(read.NewLine == ending); Assert(!doc.Dirty && !read.Dirty);
        });
Test("UTF-8 saves without a BOM by default", () => { var d = new Document { Text = "hello" }; string p = Path.Combine(root, "nobom.txt"); TextFiles.Save(d, p); Assert(File.ReadAllBytes(p).SequenceEqual("hello"u8.ToArray())); });
Test("Unchanged mixed line endings preserved byte-for-byte", () => { string p = Path.Combine(root, "mixed.txt"); File.WriteAllText(p, "a\r\nb\nc\r"); byte[] original = File.ReadAllBytes(p); var d = TextFiles.Open(p); TextFiles.Save(d, p); Assert(original.SequenceEqual(File.ReadAllBytes(p))); });
Test("External changes are not silently overwritten", () => { string p = Path.Combine(root, "conflict.txt"); File.WriteAllText(p, "one"); var d = TextFiles.Open(p); File.WriteAllText(p, "external"); d.Text = "local"; bool rejected = false; try { TextFiles.Save(d, p); } catch (IOException) { rejected = true; } Assert(rejected && File.ReadAllText(p) == "external" && d.Dirty); });
Test("Explicit conflict resolution saves local contents", () => { string p = Path.Combine(root, "resolve.txt"); File.WriteAllText(p, "one"); var d = TextFiles.Open(p); File.WriteAllText(p, "external"); d.Text = "local"; TextFiles.Save(d, p, true); Assert(File.ReadAllText(p) == "local"); });
Test("Unrepresentable ANSI text fails without damaging original", () => { string p = Path.Combine(root, "ansi.txt"); File.WriteAllText(p, "safe"); var d = TextFiles.Open(p); d.EncodingName = "ANSI"; d.Text = "😀"; bool rejected = false; try { TextFiles.Save(d, p); } catch (EncoderFallbackException) { rejected = true; } Assert(rejected && File.ReadAllText(p) == "safe"); });
Test("Read-only file remains intact on failed save", () => { string p = Path.Combine(root, "readonly.txt"); File.WriteAllText(p, "safe"); var d = TextFiles.Open(p); d.Text = "changed"; File.SetAttributes(p, FileAttributes.ReadOnly); try { try { TextFiles.Save(d, p); } catch (UnauthorizedAccessException) { } catch (IOException) { } Assert(File.ReadAllText(p) == "safe" && d.Dirty); } finally { File.SetAttributes(p, FileAttributes.Normal); } });
Test("Whole-word search excludes substrings and underscores", () => { var m = SearchEngine.FindAll("cat Cat scatter cat_ cat.", "cat", false, true); Assert(m.Count == 3); });
Test("Case-sensitive search", () => Assert(SearchEngine.FindAll("Cat cat CAT", "cat", true, false).Count == 1));
Test("Empty search terminates with no matches", () => Assert(SearchEngine.FindAll("abc", "", false, false).Count == 0));
Test("Replacement treats dollar signs and backslashes literally", () => { string r = SearchEngine.ReplaceAll("abc abc", "abc", "$1\\n", false, false, out int n); Assert(n == 2 && r == "$1\\n $1\\n"); });
Test("Numbered names remain stable after typing", () => { var d = new Document { UntitledNumber = 9, Text = "different heading" }; Assert(d.Name == "Text 9"); });
Test("Caret offsets map correctly across CRLF and LF", () => { const string raw = "a\r\nbb\nccc\r\nd"; int rawOffset = raw.IndexOf('d'); int normalized = TextFiles.ToNormalizedOffset(raw, rawOffset); Assert(normalized == TextFiles.Normalize(raw).IndexOf('d')); Assert(TextFiles.FromNormalizedOffset(raw, normalized) == rawOffset); });
Test("Sequential new documents and persistent next value", () => { var s = new Settings(); Assert(DocumentFactory.Create(s).Name == "Text 1"); Assert(DocumentFactory.Create(s).Name == "Text 2"); var store = new Store(Path.Combine(root, "sequence")); store.Write("settings.json", s); var restored = store.Read<Settings>("settings.json"); Assert(DocumentFactory.Create(restored).Name == "Text 3"); });
Test("Auto-save creates empty files at creation", () => { var s = new Settings { AutoSaveDirectory = Path.Combine(root, "new") }; var d = DocumentFactory.Create(s); Assert(d.AutoSave && d.Path != null && File.Exists(d.Path) && new FileInfo(d.Path).Length == 0 && d.Name == "Text 1.txt"); });
Test("Reset skips occupied file numbers without overwrite", () => { string dir = Path.Combine(root, "collision"); Directory.CreateDirectory(dir); File.WriteAllText(Path.Combine(dir, "Text 1.txt"), "keep"); var s = new Settings { AutoSaveDirectory = dir, NextDocumentNumber = 1 }; var d = DocumentFactory.Create(s); Assert(d.UntitledNumber == 2 && s.NextDocumentNumber == 3); Assert(File.ReadAllText(Path.Combine(dir, "Text 1.txt")) == "keep"); });
Test("Atomic session backup recovers corrupt primary", () => { var store = new Store(Path.Combine(root, "backup")); store.Write("s.json", new Settings { NextDocumentNumber = 17 }); store.Write("s.json", new Settings { NextDocumentNumber = 18 }); File.WriteAllText(Path.Combine(store.DirectoryPath, "s.json"), "{"); Assert(store.Read<Settings>("s.json").NextDocumentNumber == 17); });
Test("Session preserves unsaved text, caret, zoom, list width", () => { var d = new Document { Text = "unsaved\n中文", Caret = 4, Zoom = 170 }; var session = new Session { Windows = [new WindowSession { Documents = [d], DocumentList = true, ListWidth = 333 }] }; var store = new Store(Path.Combine(root, "session")); store.Write("s.json", session); var read = store.Read<Session>("s.json").Windows[0]; Assert(read.Documents[0].Text == d.Text && read.Documents[0].Dirty && read.Documents[0].Caret == 4 && read.Documents[0].Zoom == 170 && read.ListWidth == 333 && read.DocumentList); });
Test("Binary content does not open as text", () => { string p = Path.Combine(root, "binary"); File.WriteAllBytes(p, [1, 0, 2, 0]); bool rejected = false; try { TextFiles.Open(p); } catch (InvalidDataException) { rejected = true; } Assert(rejected); });
Test("Large text saves and reopens without truncation", () => { var d = new Document { Text = string.Concat(Enumerable.Repeat("line with unicode 中文\n", 100000)) }; string p = Path.Combine(root, "large.txt"); TextFiles.Save(d, p); Assert(TextFiles.Open(p).Text == d.Text); });
Console.WriteLine($"\n{passed} passed, {failed} failed");
// Only the unique directory created by this process is removed.
Directory.Delete(root, true);
return failed == 0 ? 0 : 1;
