using RoR2;
using RoR2.Projectile;
using EntityStates;
using EntityStates.NullifierMonster;
using System.Linq;
using UnityEngine;

namespace AugmentedVoidReaver
{
  public class BetterPortalBomb : BaseState
  {
    private HurtBox target;
    public static float baseDuration;
    public static float arcMultiplier;
    private float duration;
    private Predictor predictor;
    private Vector3 pointA;
    private Vector3 pointB;
    private int bombsFired;
    private float fireTimer;
    private float fireInterval;
    private Vector3 predictedTargetPosition;
    private Vector3 lastBombPosition;

    public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;

    public override void OnEnter()
    {
      base.OnEnter();
      if (this.isAuthority)
      {
        BullseyeSearch bullseyeSearch = new BullseyeSearch();
        bullseyeSearch.viewer = this.characterBody;
        bullseyeSearch.searchOrigin = this.characterBody.corePosition;
        bullseyeSearch.searchDirection = this.characterBody.corePosition;
        bullseyeSearch.maxDistanceFilter = FirePortalBomb.maxDistance;
        bullseyeSearch.teamMaskFilter = TeamMask.GetEnemyTeams(this.GetTeam());
        bullseyeSearch.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
        bullseyeSearch.RefreshCandidates();
        this.target = bullseyeSearch.GetResults().FirstOrDefault<HurtBox>();
        if ((bool)(Object)this.target)
        {
          this.predictor = new Predictor(this.transform);
          this.predictor.SetTargetTransform(target.transform);
        }
      }
      this.duration = AimPortalBomb.baseDuration;
    }

    public override void FixedUpdate()
    {
      base.FixedUpdate();
      if ((bool)(Object)this.target)
      {
        Ray aimRay = this.GetAimRay();
        this.pointA = this.target.transform.position;
        this.predictor.aimRay = aimRay;
        this.predictor.GetPredictedTargetPosition(1f, out this.predictedTargetPosition);
        this.fireTimer -= Time.fixedDeltaTime;
        if ((double)this.fireTimer <= 0.0)
        {
          this.fireTimer += this.fireInterval;
          {
            float t = (float)this.bombsFired * (float)(1.0 / ((double)FirePortalBomb.portalBombCount - 1.0));
            this.FireBomb(aimRay);
            EffectManager.SimpleMuzzleFlash(FirePortalBomb.muzzleflashEffectPrefab, this.gameObject, FirePortalBomb.muzzleString, true);
          }
          ++this.bombsFired;
        }
      }
      if ((double)this.fixedAge < (double)this.duration)
        return;
      this.outer.SetNextStateToMain();
    }

    private void FireBomb(Ray fireRay)
    {
      Vector3 point = this.predictedTargetPosition;
      float x = Random.Range(-10, 10);
      float y = Random.Range(0, 5);
      float z = Random.Range(-10, 10);
      Vector3 directionWithSpread = point + new Vector3(x, y, z);
      ProjectileManager.instance.FireProjectile(new FireProjectileInfo()
      {
        projectilePrefab = FirePortalBomb.portalBombProjectileEffect,
        position = directionWithSpread,
        rotation = Quaternion.identity,
        owner = this.gameObject,
        damage = this.damageStat * FirePortalBomb.damageCoefficient,
        force = FirePortalBomb.force,
        crit = this.characterBody.RollCrit()
      });
      this.lastBombPosition = point;
    }

    private class Predictor
    {
      public Ray aimRay;
      private Transform bodyTransform;
      private Transform targetTransform;
      private Vector3 targetPosition0;
      private Vector3 targetPosition1;
      private Vector3 targetPosition2;
      private int collectedPositions;

      public Predictor(Transform bodyTransform) => this.bodyTransform = bodyTransform;

      public bool hasTargetTransform => (bool)(Object)this.targetTransform;

      public bool isPredictionReady => this.collectedPositions > 2;

      private void PushTargetPosition(Vector3 newTargetPosition)
      {
        this.targetPosition2 = this.targetPosition1;
        this.targetPosition1 = this.targetPosition0;
        this.targetPosition0 = newTargetPosition;
        ++this.collectedPositions;
      }

      public void SetTargetTransform(Transform newTargetTransform)
      {
        this.targetTransform = newTargetTransform;
        this.targetPosition2 = this.targetPosition1 = this.targetPosition0 = newTargetTransform.position;
        this.collectedPositions = 1;
      }

      public void Update()
      {
        if (!(bool)(Object)this.targetTransform)
          return;
        this.PushTargetPosition(this.targetTransform.position);
      }

      public bool GetPredictedTargetPosition(float time, out Vector3 predictedPosition)
      {
        Vector3 vector3_1 = this.targetPosition1 - this.targetPosition2;
        Vector3 vector3_2 = this.targetPosition0 - this.targetPosition1;
        vector3_1.y = 0.0f;
        vector3_2.y = 0.0f;
        Predictor.ExtrapolationType extrapolationType = vector3_1 == Vector3.zero || vector3_2 == Vector3.zero ? Predictor.ExtrapolationType.None : ((double)Vector3.Dot(vector3_1.normalized, vector3_2.normalized) <= 0.980000019073486 ? Predictor.ExtrapolationType.Polar : Predictor.ExtrapolationType.Linear);
        float num1 = 1f / Time.fixedDeltaTime;
        predictedPosition = this.targetPosition0;
        switch (extrapolationType)
        {
          case Predictor.ExtrapolationType.Linear:
            predictedPosition = this.targetPosition0 + vector3_2 * (time * num1);
            break;
          case Predictor.ExtrapolationType.Polar:
            Vector3 position = this.bodyTransform.position;
            Vector3 vector2Xy1 = (Vector3)Util.Vector3XZToVector2XY(this.targetPosition2 - position);
            Vector3 vector2Xy2 = (Vector3)Util.Vector3XZToVector2XY(this.targetPosition1 - position);
            Vector3 vector2Xy3 = (Vector3)Util.Vector3XZToVector2XY(this.targetPosition0 - position);
            float magnitude1 = vector2Xy1.magnitude;
            float magnitude2 = vector2Xy2.magnitude;
            float magnitude3 = vector2Xy3.magnitude;
            float num2 = Vector2.SignedAngle((Vector2)vector2Xy1, (Vector2)vector2Xy2) * num1;
            float num3 = Vector2.SignedAngle((Vector2)vector2Xy2, (Vector2)vector2Xy3) * num1;
            double num4 = ((double)magnitude2 - (double)magnitude1) * (double)num1;
            float num5 = (magnitude3 - magnitude2) * num1;
            float num6 = (float)(((double)num2 + (double)num3) * 0.5);
            double num7 = (double)num5;
            float num8 = (float)((num4 + num7) * 0.5);
            float num9 = magnitude3 + num8 * time;
            if ((double)num9 < 0.0)
              num9 = 0.0f;
            Vector2 vector2 = Util.RotateVector2((Vector2)vector2Xy3, num6 * time) * (num9 * magnitude3);
            predictedPosition = position;
            predictedPosition.x += vector2.x;
            predictedPosition.z += vector2.y;
            break;
        }
        return true;
      }
      private enum ExtrapolationType
      {
        None,
        Linear,
        Polar,
      }
    }
  }
}