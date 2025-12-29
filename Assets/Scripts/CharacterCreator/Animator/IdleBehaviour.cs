using UnityEngine;

namespace Game.CharacterCreator
{
    public class IdleBehaviour : StateMachineBehaviour
    {
        public string IdleIntName = "Idle";
        public float checkTimer = 3F;
        public int[] RandomIdleID;
        public int ChanceToPlayRandomIdle = 20;
        private float _timer = 0F;
        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (_timer < checkTimer)
            {
                _timer += Time.deltaTime;
            }
            else
            {
                _timer = Random.Range(-8F, -3F);
                if (Random.Range(0, 100) < ChanceToPlayRandomIdle)
                {
                    animator.SetInteger(IdleIntName, RandomIdleID[Random.Range(0, RandomIdleID.Length)]);
                }
            }
        }

        override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.SetInteger(IdleIntName, 0);
        }

    }
}
