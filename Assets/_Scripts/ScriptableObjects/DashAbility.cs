using System.Collections;
using System.Collections.Generic;
using ExtensionMethods;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDashAbility", menuName = "Assets/Abilities/Dash Ability")]
public class DashAbility : Ability {
    public float dashDistance;

    public AbilityState abilityState = AbilityState.Ready;

    public override void Activate(GameObject agent) {
        PlayerController movement = agent.GetComponent<PlayerController>();
    }

    public override void Activate2(PlayerController controller) {
        //controller.DodgeCoroutine = controller.StartCoroutine(DodgeRoll(controller, 1f));
    }

    private IEnumerator DodgeRoll(PlayerController controller, float targetTime) {
        float elapsedTime = 0f;

        controller.playerInputs.move.Disable();
        controller.playerInputs.swing.Disable();
        controller.playerInputs.dodge.Disable();

        Vector2 mousePosition = Utils.ScreenToWorld(Utils.GetMainCamera(), Input.mousePosition);
        Vector2 currentPosition = controller.transform.position;
        Vector2 targetDirection = mousePosition.normalized * dashDistance + (Vector2)controller.transform.position;

        while (elapsedTime < targetTime) {
            Debug.Log("Player is Dodge rolling!");
            /*currentPosition = Vector2.Lerp(currentPosition, targetDirection, elapsedTime);
            transform.position = currentPosition;*/
            controller.transform.position = Vector2.Lerp(controller.transform.position, targetDirection, elapsedTime);
            
            elapsedTime += Time.deltaTime;

            if ((Vector2)controller.transform.position == targetDirection) {
                controller.playerInputs.move.Enable();
                controller.playerInputs.swing.Enable();
                controller.playerInputs.dodge.Enable();

                controller.transform.position = targetDirection;
                break;
            }

            //yield return controller.waitForEndOfFrame;
            yield return null;
        }
    }
}
