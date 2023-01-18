using BepInEx;
using RoR2;
using RoR2.Skills;
using RoR2.CharacterAI;
using R2API;
using R2API.Utils;
using EntityStates;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Linq;

namespace AugmentedVoidReaver
{
  [BepInPlugin("com.Nuxlar.AugmentedVoidReaver", "AugmentedVoidReaver", "2.0.0")]
  [BepInDependency("com.bepis.r2api.content_management", BepInDependency.DependencyFlags.HardDependency)]

  public class AugmentedVoidReaver : BaseUnityPlugin
  {
    private static GameObject voidReaver = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Nullifier/NullifierBody.prefab").WaitForCompletion();
    private static GameObject voidReaverMaster = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Nullifier/NullifierMaster.prefab").WaitForCompletion();

    private void Awake()
    {
      CreateSecondary();
      CreateSecondaryDrivers();
      On.RoR2.CharacterMaster.OnBodyStart += CharacterMaster_OnBodyStart;
    }

    private void CreateSecondaryDrivers()
    {
      CharacterMaster master = voidReaverMaster.GetComponent<CharacterMaster>();
      AISkillDriver[] primarySkillDrivers = master.GetComponents<AISkillDriver>().Where<AISkillDriver>((x => x.skillSlot == SkillSlot.Primary)).ToArray();
      foreach (AISkillDriver primaryDriver in primarySkillDrivers)
      {
        if (primaryDriver.customName == "PanicFireWhenClose")
        {
          primaryDriver.skillSlot = SkillSlot.Secondary;
          primaryDriver.maxDistance = 22f;
        }
        if (primaryDriver.customName == "FireAndStrafe")
        {
          primaryDriver.maxDistance = 66f;
          primaryDriver.minDistance = 22f;
        }
        if (primaryDriver.customName == "FireAndChase")
        {
          primaryDriver.skillSlot = SkillSlot.Secondary;
          primaryDriver.minDistance = 66f;
        }
      }
    }

    private void CharacterMaster_OnBodyStart(On.RoR2.CharacterMaster.orig_OnBodyStart orig, RoR2.CharacterMaster self, CharacterBody body)
    {
      orig(self, body);
      if (body.name == "NullifierBody(Clone)")
      {
        body.inventory.GiveItem(RoR2Content.Items.LunarPrimaryReplacement);
        body.skillLocator.primary.skillDef.baseMaxStock = 3;
      }
    }

    private void CreateSecondary()
    {
      // Create GenericSkill and SkillFamily
      SkillLocator skillLocator = voidReaver.GetComponent<SkillLocator>();
      skillLocator.secondary = voidReaver.AddComponent<GenericSkill>();
      SkillFamily skillFamily = ScriptableObject.CreateInstance<SkillFamily>();
      skillFamily.variants = new SkillFamily.Variant[1];
      Reflection.SetFieldValue<SkillFamily>((object)skillLocator.secondary, "_skillFamily", skillFamily);
      // Create SkillDef
      SkillDef betterPortalBomb = ScriptableObject.CreateInstance<SkillDef>();
      betterPortalBomb.activationState = new EntityStates.SerializableEntityStateType(typeof(BetterPortalBomb));
      betterPortalBomb.baseMaxStock = 1;
      betterPortalBomb.baseRechargeInterval = 5;
      betterPortalBomb.rechargeStock = 1;
      betterPortalBomb.activationStateMachineName = "Body";
      betterPortalBomb.beginSkillCooldownOnSkillEnd = true;
      betterPortalBomb.canceledFromSprinting = false;
      betterPortalBomb.cancelSprintingOnActivation = true;
      betterPortalBomb.fullRestockOnAssign = false;
      betterPortalBomb.interruptPriority = InterruptPriority.Skill;
      betterPortalBomb.isCombatSkill = true;
      betterPortalBomb.mustKeyPress = false;
      betterPortalBomb.requiredStock = 1;
      betterPortalBomb.stockToConsume = 1;
      betterPortalBomb.skillName = "BetterPortalBomb";
      // Create Variant
      SkillFamily.Variant[] variants = skillLocator.secondary.skillFamily.variants;
      SkillFamily.Variant variant = new();
      variant.skillDef = betterPortalBomb;
      variants[0] = variant;

      ContentAddition.AddSkillDef(betterPortalBomb);
      ContentAddition.AddSkillFamily(skillFamily);
    }

  }
}