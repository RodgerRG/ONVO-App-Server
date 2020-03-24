using System.Collections.Generic;
using ONVO_App.Structs;
using System;
namespace ONVO_App.CombatSimulator
{
    public class Combat
    {
        private List<CombatCharacter> combatants;
        private List<Character> players;
        private List<Character> enemies;
        private int roundNumber;

        public Combat() {
            combatants = new List<CombatCharacter>();
            players = new List<Character>();
            enemies = new List<Character>();
        }

        public void addPlayer(Character c) {
            CombatCharacter character = new CombatCharacter(c);
            if(!combatants.Contains(character)) {
                combatants.Add(character);
                players.Add(c);
            }
        }

        public void addEnemy(Character c) {
            CombatCharacter character = new CombatCharacter(c);
            if(!combatants.Contains(character)) {
                combatants.Add(character);
                enemies.Add(c);
            }
        }

        public void resolveActions(List<Action> actions) {
            resolveBlights();
            
            foreach(Action a in actions) {
                Character source = a.getSource();
                Character target = a.getTarget();
                Skill skill = a.GetSkill();

                bool isJousting = false;
                foreach(Action act in actions) {
                    //resolve DOT effects for the character acting
                    resolveDOTs(act.getSource());
                    //detect if a joust is potentially occurring. 
                    if(act.getSource() == target && act.getTarget() == source && !((players.Contains(act.getSource()) && players.Contains(act.getTarget())) || (enemies.Contains(act.getSource()) && enemies.Contains(act.getTarget())))) {
                        isJousting = true;
                        resolveJoust(a, act);
                        actions.Remove(act);
                        break;
                    }
                }

                if(!isJousting) {
                    resolveAction(a);
                }
            }
        }

        private void resolveBlights() {
            foreach(CombatCharacter c in combatants) {
                c.applyBlight();
            }
        }

        private void resolveDOTs(Character c) {
            foreach(CombatCharacter character in combatants) {
                if(character.getCharacter().Equals(c)) {
                    character.applyDOTs();
                    return;
                }
            }
        }

        private bool isRoundOver() {
            bool isOver = true;
            foreach(CombatCharacter c in combatants) {
                if(c.canMakeAction()) {
                    isOver = false;
                }
            }

            return isOver;
        }

        private void resolveJoust(Action firstChar, Action secondChar) {
            Skill firstSkill = firstChar.GetSkill();
            Skill secondSkill = secondChar.GetSkill();

            string[] firstKeywords = firstSkill.getKeywords();
            string[] secondKeywords = secondSkill.getKeywords();

            JoustType firstJoust = JoustType.MELEE; 
            foreach(string s in firstKeywords) {
                if(s == "Overwhelm" || s == "Odd" || s == "Ranged") {
                    firstJoust = stringToJoustType(s);
                }
            }

            JoustType secondJoust = JoustType.MELEE;
            foreach(string s  in secondKeywords) {
                if(s == "Overwhelm" || s == "Odd" || s == "Ranged") {
                    secondJoust = stringToJoustType(s);
                }
            }

            if(firstJoust > secondJoust) {
                resolveAction(firstChar);
            } else if(firstJoust == secondJoust) {
                //find the damage of the skills, and apply the effects of both skills.
                int firstDamage = firstSkill.getDamage();
                int secondDamage = secondSkill.getDamage();

                Skill newFirstSkill = new Skill(firstSkill, Math.Max(firstDamage - secondDamage, 0));
                Skill newSecondSkill = new Skill(secondSkill, Math.Max(secondDamage - firstDamage, 0));

                resolveAction(new Action(firstChar.getSource(), firstChar.getTarget(), newFirstSkill));
                resolveAction(new Action(secondChar.getSource(), secondChar.getTarget(), newSecondSkill));
            } else {
                resolveAction(secondChar);
            }
        }

        private void resolveAction(Action action) {
            Character target = action.getTarget();
            CombatCharacter combatant = null;
            foreach(CombatCharacter c in combatants) {
                if(c.getCharacter() == target) {
                    combatant = c;
                    break;
                }
            }

            Skill skill = action.GetSkill();
            combatant.addBleed(skill.getBleed());
            combatant.addBlight(skill.getBlight());
            combatant.addBurn(skill.getBurn());
            combatant.applyDamage(skill.getDamage());

            resolveHidden(action);
        }

        private void resolveHidden(Action action) {
            //if the source character of the skill is hidden, they are no longer hidden unless the skill is also hidden or the target is themselves.
            if(action.getSource() != action.getTarget()) {
                if(!action.GetSkill().getHidden()) {
                    foreach(CombatCharacter character in combatants) {
                        if(character.getCharacter().Equals(action.getSource())) {
                            if(character.getHidden()) {
                                character.toggleHidden();
                                break;
                            }
                        }
                    }
                }
            }

            //resolve if the skill would dispel hidden from the target.
            foreach(CombatCharacter c in combatants) {
                if(c.getCharacter().Equals(action.getTarget())) {
                    if(c.getHidden() && action.GetSkill().IsDispel() && action.GetSkill().getHidden()) {
                        c.toggleHidden();
                    } else if(!c.getHidden() && action.GetSkill().getHidden()) {
                        c.toggleHidden();
                    }
                }
            }
        }

        private JoustType stringToJoustType(string s) {
            switch(s) {
                case "Ranged":
                return JoustType.RANGED;
                case "Overwhelm":
                return JoustType.OVERWHELM;
                case "Odd":
                return JoustType.ODD;
                default:
                //this should never trigger, but hey this is needed to compile.
                return JoustType.MELEE;
            }
        }

        private enum JoustType {
            ODD = 4, 
            OVERWHELM = 3, 
            RANGED = 2, 
            MELEE = 1
        }
    }
}