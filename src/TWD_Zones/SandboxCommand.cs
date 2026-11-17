// Commande console admin "twdsandbox" : génère le SandboxCode avec la fonction du jeu lui-même.
//   twdsandbox list            liste toutes les options, leur valeur actuelle et les valeurs possibles
//                              (écrit aussi twd_sandbox_options.txt dans le dossier du mod)
//   twdsandbox code            code correspondant aux options actuellement chargées
//   twdsandbox apply <fichier> applique "Option=valeur" (une par ligne, valeur = texte affiché ou index),
//                              puis affiche le code. Les options absentes du fichier gardent leur valeur actuelle.
// N'écrit PAS dans serverconfig.xml : le code obtenu y est copié à la main (SandboxCode).

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SbMgr = global::SandboxOptions.SandboxOptionManager;
using SbOpt = global::SandboxOptions.SandboxOptions;
using SbOption = global::SandboxOptions.BaseSandboxOption;
using SbPreset = global::SandboxOptions.SandboxOptionPreset;

namespace TWDZones
{
    public class SandboxCommand : ConsoleCmdAbstract
    {
        public override string[] getCommands() { return new string[] { "twdsandbox" }; }
        public override string getDescription() { return "TWD : génération du SandboxCode"; }
        public override string getHelp() { return "twdsandbox list | code | apply <fichier>"; }

        static void Out(string s) { SdtdConsole.Instance.Output(s); }
        static string ModDir() { return Path.GetDirectoryName(ZoneStore.FilePath); }

        // onlyChanged = true : seulement les options différentes de leur valeur par défaut (forme courte, comme le jeu)
        static string CurrentCode(bool onlyChanged)
        {
            var p = new SbPreset();          // PresetValues est créé par le constructeur (champ en lecture seule)
            p.PresetValues.Clear();
            foreach (KeyValuePair<SbOpt, SbOption> kv in SbMgr.Current.SandboxOptionsDict)
                if (!onlyChanged || kv.Value.GetValueIndex() != kv.Value.GetDefaultIndex())
                    p.PresetValues[kv.Key] = kv.Value.GetValueIndex();
            return p.SandboxCode;
        }

        static string AllValues(SbOption o)
        {
            var vals = new List<string>();
            if (o.ValueOptions == null) return "";
            for (int i = 0; i < 64; i++)
            {
                object v = null;
                if (!o.ValueOptions.GetValue(i, out v)) break;
                string label = (o.ValueOptions.DisplayValues != null && i < o.ValueOptions.DisplayValues.Length) ? o.ValueOptions.DisplayValues[i] : null;
                vals.Add(i + ":" + Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) + (string.IsNullOrEmpty(label) || label == "none" ? "" : " (" + label + ")"));
            }
            return string.Join(" | ", vals.ToArray());
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            if (!SbMgr.HasInstance) { Out("Options Sandbox non initialisées."); return; }
            string sub = _params.Count > 0 ? _params[0].ToLowerInvariant() : "code";
            var mgr = SbMgr.Current;

            if (sub == "list")
            {
                var sb = new StringBuilder();
                foreach (KeyValuePair<SbOpt, SbOption> kv in mgr.SandboxOptionsDict)
                {
                    SbOption o = kv.Value;
                    sb.AppendLine(kv.Key + " = [" + o.GetValueIndex() + "]   (défaut " + o.GetDefaultIndex() + ")   " + AllValues(o));
                }
                string path = Path.Combine(ModDir(), "twd_sandbox_options.txt");
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
                Out(mgr.SandboxOptionsDict.Count + " options écrites dans " + path);
            }
            else if (sub == "code")
            {
                Out("SandboxCode (court) = " + CurrentCode(true));
                Out("SandboxCode (complet) = " + CurrentCode(false));
            }
            else if (sub == "apply" && _params.Count >= 2)
            {
                string file = _params[1];
                if (!Path.IsPathRooted(file)) file = Path.Combine(ModDir(), file);
                if (!File.Exists(file)) { Out("Fichier introuvable : " + file); return; }
                int ok = 0;
                foreach (string raw in File.ReadAllLines(file))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    int eq = line.IndexOf('=');
                    if (eq < 0) { Out("Ligne ignorée : " + line); continue; }
                    string name = line.Substring(0, eq).Trim(), value = line.Substring(eq + 1).Trim();
                    int hash = value.IndexOf('#'); if (hash >= 0) value = value.Substring(0, hash).Trim();
                    SbOpt opt;
                    try { opt = (SbOpt)Enum.Parse(typeof(SbOpt), name, true); }
                    catch { Out("Option inconnue : " + name); continue; }
                    SbOption o = SbMgr.GetOption(opt);
                    // valeur = index de la liste (voir "twdsandbox list"), par ex. ZombieMove=0
                    int idx; object probe = null;
                    if (!int.TryParse(value, out idx) || o.ValueOptions == null || !o.ValueOptions.GetValue(idx, out probe))
                    { Out("Valeur invalide pour " + name + " : " + value + " (attendu : un index de 'twdsandbox list')"); continue; }
                    o.SetValueFromIndex(idx);
                    ok++;
                }
                Out(ok + " option(s) appliquée(s).");
                Out("SandboxCode = " + CurrentCode(true));
            }
            else Out(getHelp());
        }
    }
}
