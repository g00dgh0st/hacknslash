using System.Collections;
using System.Collections.Generic;
using ofr.grim;
using UnityEngine;
namespace ofr.grim.character {

  public class EnemyAttackMachine : StateMachineBehaviour {
    // OnStateEnter is called before OnStateEnter is called on any state inside this state machine
    // override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
    //   AttackState state;

    //   if (stateInfo.IsTag("Swing")) {
    //     state = AttackState.Swing;
    //   } else if (stateInfo.IsTag("Continue")) {
    //     state = AttackState.Continue;
    //   } else {
    //     state = AttackState.End;

    //   }

    //   animator.GetComponent<PlayerController>().AttackMachineCallback(state);
    // }

    // OnStateUpdate is called before OnStateUpdate is called on any state inside this state machine
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateExit is called before OnStateExit is called on any state inside this state machine
    // override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
    //   Debug.Log("EXITTTTTTT");
    //   Debug.Log(stateInfo.IsTag("Swing"));
    //   Debug.Log(stateInfo.IsTag("Continue"));
    // }

    // OnStateMove is called before OnStateMove is called on any state inside this state machine
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateIK is called before OnStateIK is called on any state inside this state machine
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMachineEnter is called when entering a state machine via its Entry Node
    override public void OnStateMachineEnter(Animator animator, int stateMachinePathHash) {
      animator.GetComponent<EnemyController>().AttackMachineCallback(true);
    }

    // OnStateMachineExit is called when exiting a state machine via its Exit Node
    override public void OnStateMachineExit(Animator animator, int stateMachinePathHash) {
      animator.GetComponent<EnemyController>().AttackMachineCallback(false);
    }
  }
}