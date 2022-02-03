using System;
using System.Collections.Generic;
using XRL.Rules;
using XRL.UI;
using XRL.World.Effects;
using XRL.World.Parts.Skill;

namespace XRL.World.Parts
{
	[Serializable]
	public class Amputator : IPart
	{
		public override bool WantEvent(int ID, int cascade)
		{
			return base.WantEvent(ID, cascade) || ID == AfterInventoryActionEvent.ID
				|| ID == GetInventoryActionsAlwaysEvent.ID;
		}

		public override bool HandleEvent(GetInventoryActionsAlwaysEvent E)
		{
			if (E.Object.IsEquippedProperly())
			{
				E.AddAction("Amputate", "amputate", "Amputate", Key: 'P', Default: -100);
			}
			return base.HandleEvent(E);
		}

		public override bool HandleEvent(AfterInventoryActionEvent E)
		{
			if (E.Command == "Amputate" && E.Actor != null && E.Actor.IsPlayer() && E.Actor.Body != null)
			{
				bool isCancelled = false;
				bool isSuccess = true;
				var domination = E.Actor.GetEffect<Dominated>();

				if (domination != null)
				{
					int bonus = Stat.Roll("1d8-6");
					int attack = domination.Dominator.Level + domination.Dominator.StatMod("Willpower") + bonus;
					int defense = E.Actor.Level + Stats.GetCombatMA(E.Actor);
					isSuccess = attack >= defense;
				}

				if (isSuccess)
				{
					var dismemberableParts = new List<BodyPart>();
					var partNames = new List<string>();
					List<BodyPart> parts = E.Actor.Body.GetParts();

					foreach (BodyPart part in parts)
					{
						if (Axe_Dismember.BodyPartIsDismemberable(part, E.Actor, true))
						{
							dismemberableParts.Add(part);
							partNames.Add(part.Name);
						}
					}

					int index = Popup.ShowOptionList("Amputation", partNames.ToArray(), AllowEscape: true,
						Intro: "Which body part would you like to amputate?");
					if (index != -1)
					{
						string name = partNames[index];
						BodyPart part = dismemberableParts[index];
						string message = "Are you sure you want to amputate your " + name + "?";
						DialogResult confirmation = Popup.ShowYesNo(message, true, DialogResult.No);
						if (confirmation == DialogResult.Yes)
						{
							Axe_Dismember.Dismember(E.Actor, E.Actor, LostPart: part, Weapon: E.Item);
						}
						else
						{
							isCancelled = true;
						}
					}
					else
					{
						isCancelled = true;
					}
				}
				else
				{
					Popup.Show("The body resists your command to mutilate itself.");
				}

				if (!isCancelled)
				{
					E.Actor.UseEnergy(1000, "Amputation");
				}

				E.RequestInterfaceExit();
			}
			return base.HandleEvent(E);
		}
	}
}
