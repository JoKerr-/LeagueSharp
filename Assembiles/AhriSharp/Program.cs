using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

//      WIP
//      Add some sort of R support
//      Credits: Many L# Devs, I'm a shit coder just wanted to play about as i love Ahri.

namespace AhriSharp
{
    class Program
    {
        private const string ChampionName = "Ahri";
        private static readonly Obj_AI_Hero player = ObjectManager.Player;
        private static Orbwalking.Orbwalker Orbwalker;
        private static Spell Q, W, E, R;
        private static Items.Item DFG;
        private static bool hasIgnite = false;
        private static SpellSlot igniteSlot;

        private static Menu Config;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != ChampionName)
            {
                return;
            }
            
            Q = new Spell(SpellSlot.Q, 860);
            W = new Spell(SpellSlot.W, 800);
            E = new Spell(SpellSlot.E, 960);
            R = new Spell(SpellSlot.R, 450); //Soon(TM)
            DFG = new Items.Item(3128, 750);

            Q.SetSkillshot(0.50f, 100f, 1100f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.50f, 60f, 1200f, true, SkillshotType.SkillshotLine);

            //Ignite check //Hellsing
            var ignite = player.Spellbook.GetSpell(player.GetSpellSlot("SummonerDot"));
            if (ignite != null && ignite.Slot != SpellSlot.Unknown)
            {
                hasIgnite = true;
                igniteSlot = ignite.Slot;
            }

            //Menu
            Config = new Menu("AhriSharp", "Ahri", true);


            //Orbwalker & Target Selector
            Config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            //Combo
            Config.AddSubMenu(new Menu("Combo", "combo"));
            Config.SubMenu("combo").AddItem(new MenuItem("UseQ", "Use Q")).SetValue(true);
            Config.SubMenu("combo").AddItem(new MenuItem("UseW", "Use W")).SetValue(true);
            Config.SubMenu("combo").AddItem(new MenuItem("UseE", "Use E")).SetValue(true);
            Config.SubMenu("combo").AddItem(new MenuItem("UseDFG", "DFG on Charmed Target")).SetValue(true);
            //Config.SubMenu("combo").AddItem(new MenuItem("UseR", "Use R")).SetValue(false); Soon(TM)
            Config.SubMenu("combo").AddItem(new MenuItem("ActiveCombo", "Combo").SetValue(new KeyBind(32, KeyBindType.Press)));

            //Harras
            Config.AddSubMenu(new Menu("Harras", "harras"));
            Config.SubMenu("harras").AddItem(new MenuItem("UseQh", "Use Q")).SetValue(true);
            Config.SubMenu("harras").AddItem(new MenuItem("ActiveHarras", "Harras").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            //LaneClear
            Config.AddSubMenu(new Menu("LaneClear", "laneClear")); //Add Mana Option.
            Config.SubMenu("laneClear").AddItem(new MenuItem("UseQc", "Use Q")).SetValue(true);
            Config.SubMenu("laneClear").AddItem(new MenuItem("UseWc", "Use W")).SetValue(false);
            Config.SubMenu("laneClear").AddItem(new MenuItem("LMana", "Min Mana").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("laneClear").AddItem(new MenuItem("ActiveLane", "Lane Clear").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            //JungleClear
            Config.AddSubMenu(new Menu("JungleClear", "jungleClear"));
            Config.SubMenu("jungleClear").AddItem(new MenuItem("UseQj", "Use Q")).SetValue(true);
            Config.SubMenu("jungleClear").AddItem(new MenuItem("UseWj", "Use W")).SetValue(false);
            Config.SubMenu("jungleClear").AddItem(new MenuItem("ActiveJungle", "Jungle Clear").SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Press)));

            //Misc
            Config.AddSubMenu(new Menu("Misc", "misc"));
            Config.SubMenu("misc").AddItem(new MenuItem("UseI", "Ignite On Killable")).SetValue(true);


            Config.AddToMainMenu();

            Drawing.OnDraw += OnDraw;
            Game.OnGameUpdate += OnGameUpdate;
        }

        private static void OnGameUpdate(EventArgs args)
        {
            Obj_AI_Hero itarget = SimpleTs.GetTarget(600, SimpleTs.DamageType.True);
            var useI = Config.Item("UseI").GetValue<bool>();

            if (Config.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            if (Config.Item("ActiveHarras").GetValue<KeyBind>().Active)
            {
                Harras();
            }
            if (Config.Item("ActiveJungle").GetValue<KeyBind>().Active)
            {
                JungleClear();
            }
            if (Config.Item("ActiveLane").GetValue<KeyBind>().Active)
            {
                LaneClear();
            }
            if (useI && hasIgnite && player.Spellbook.GetSpell(player.GetSpellSlot("SummonerDot")).State == SpellState.Ready)
            {
                var igniteDmg = DamageLib.getDmg(itarget, DamageLib.SpellType.IGNITE);
                if (igniteDmg > itarget.Health)
                    player.SummonerSpellbook.CastSpell(igniteSlot, itarget);
            }
        }

        static void OnDraw(EventArgs args)
        {

        }

        private static void Combo()
        {
            Obj_AI_Hero target = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var useQ = Config.Item("UseQ").GetValue<bool>();
            var useW = Config.Item("UseW").GetValue<bool>();
            var useE = Config.Item("UseE").GetValue<bool>();
            if (target == null) return;


            if (useW && target.IsValidTarget(W.Range) & W.IsReady())
            {
                    W.Cast();
            }
            if (useE && target.IsValidTarget(E.Range) && E.IsReady())
            {
                    PredictionOutput ePred = E.GetPrediction(target);
                    if (ePred.Hitchance >= HitChance.High)
                        E.Cast(ePred.CastPosition);
            }
            if (target.HasBuffOfType(BuffType.Charm) && DFG.IsReady())
            {
                DFG.Cast(target);
            }
            if (useQ && target.IsValidTarget(Q.Range) && Q.IsReady())
            {
                PredictionOutput qPred = Q.GetPrediction(target);
                if (qPred.Hitchance >= HitChance.High)
                    Q.Cast(qPred.CastPosition);
            }
        }

        private static void Harras()
        {
            Obj_AI_Hero target = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var useQh = Config.Item("UseQh").GetValue<bool>();
            if (target == null) return;

            if (useQh && target.IsValidTarget(Q.Range) && Q.IsReady())
            {
                PredictionOutput qPred = Q.GetPrediction(target);
                if (qPred.Hitchance >= HitChance.High)
                    Q.Cast(qPred.CastPosition);
            }
        }

        private static void LaneClear()
        {
            var useQc = Config.Item("UseQc").GetValue<bool>();
            var useWc = Config.Item("UseWc").GetValue<bool>();
            var FMana = Config.Item("LMana").GetValue<Slider>().Value;
            var MPercent = player.Mana*100/player.MaxMana;
            var minions = MinionManager.GetMinions(player.ServerPosition, Q.Range + Q.Width);

            if (useQc && Q.IsReady() && MPercent >= FMana)
            {                
                var qpos = Q.GetLineFarmLocation(minions);
                if (qpos.MinionsHit >= 2)
                           Q.Cast(qpos.Position);
            }
            if (useWc && W.IsReady())
            {
                W.Cast();
            }
        }

        private static void JungleClear()
        {
            var useQj = Config.Item("UseQj").GetValue<bool>();
            var useWj = Config.Item("UseWj").GetValue<bool>();
            var monster = MinionManager.GetMinions(player.ServerPosition, Q.Range + Q.Width, MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (useQj && Q.IsReady())
            {
                var qmonster = Q.GetLineFarmLocation(monster);
                if (qmonster.MinionsHit >= 1)
                    Q.Cast(qmonster.Position);
            }
            if (useWj && W.IsReady())
            {
                W.Cast();
            }

        }
    }
}
