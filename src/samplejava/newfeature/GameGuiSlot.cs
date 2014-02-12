#region Using statements

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#endregion

public class GameGuiSlot : MonoBehaviour 
{
	
	#region Enums
	
	public enum HighlightState
	{
		None,
		Active,
		Choosen
	}
	
	#endregion
	
	#region Constants
	
	private const float FirstCardDistancePerspective = 0.1f;
	private const float FirstCardDistanceNonPerspective = 0.02f;
	private const float DistanceBetweenCardsPerspective = 0.02f;
	private const float DistanceBetweenCardsNonPerspective = 0.02f;
	
	private const float DiagramGlowAlpha = 255.0f/255.0f;
	private const float DiagramNormalAlpa = 75.0f/255.0f;	
	
	#endregion
	
	#region Fields
	
	private HighlightState highlight;
	private List<int> transformsID = new List<int>();
	
	#endregion
	
	#region Unity properties
	
	public bool CanContainManyCards = false;
	public List<Card> Cards = new List<Card>();
	public bool IsPerspective = true;
	public bool IsStackSlot = false;
	public bool IsCementarySlot = false;
	public bool IsTableSlot = false;
	
	public tk2dSprite Glow;
	public tk2dSprite Diagram;
	
	#endregion
	
	#region Properties
	
	public float DistanceBetweenCards
	{
		get
		{
			return (IsPerspective ? DistanceBetweenCardsPerspective : DistanceBetweenCardsNonPerspective);
		}
	}
	
	private float FirstCardDistance
	{
		get 
		{
			return (IsPerspective ? FirstCardDistancePerspective : FirstCardDistanceNonPerspective);
		}
	}
	
	#endregion
	
	#region MonoBehaviour methods
	
	public void Awake()
	{
		SetHighlighModeImmediately(HighlightState.None);
	}
	
	#endregion
	
	#region Public methods
	
	public void MoveCardIntoThisSlot(Card card, ST.UnitFunction function, ActionDelegate onFinish)
	{
		MoveCardIntoThisSlot(card, function, 4.0f, onFinish);
	}
	
	public void MoveCardIntoThisSlot(Card card, ActionDelegate onFinish)
	{
		MoveCardIntoThisSlot(card, delegate(float t) { return t; }, 4.0f, onFinish);
	}
	
	public void MoveCardIntoThisSlot(Card card, float avarageVelocity, ActionDelegate onFinish)
	{
		MoveCardIntoThisSlot(card, delegate(float t) { return t; }, avarageVelocity, onFinish);
	}
	
	public void MoveCardIntoThisSlot(Card card, ST.UnitFunction function, float avarageVelocity, ActionDelegate onFinish)
	{
		MoveCardIntoThisSlot(card, Cards.Count, function, avarageVelocity, onFinish);
	}
	
	public void MoveCardIntoThisSlot(Card card, int position, ST.UnitFunction function, float avarageVelocity, ActionDelegate onFinish)
	{		
		Transform oldParent = card.transform.parent;		
		card.transform.parent = null;
		Vector3 oldPosition = card.transform.position;
		Quaternion oldRotation = card.transform.rotation;
		card.transform.parent = oldParent;
		
		MoveCardIntoThisSlotImmediately(card, position);
		card.transform.parent = null;		
		Vector3 targetPosition = card.transform.position;
		Quaternion targetRotation = card.transform.rotation;
		
		card.transform.position = oldPosition;
		card.transform.rotation = oldRotation;
		
		float time = (targetPosition-card.transform.position).magnitude/avarageVelocity;
		
		ST.LinearPositionTransformParam positionTransform = new ST.LinearPositionTransformParam(targetPosition, time, false, function);
		TransformManager.Instance.AddTransform(card.gameObject, delegate(int id, GameObject obj, ITransform transform, TransformEventType t) 
		{
			if(t == TransformEventType.OnListFinished)
			{
				card.transform.parent = gameObject.transform;
				if(IsCementarySlot)
					card.Reinitialize();
				if(onFinish != null)
					onFinish();
			}
		}, positionTransform);
		
		ST.LinearRotationTransformParam rotationTransform = new ST.LinearRotationTransformParam(targetRotation, 0.92f*time, false, function);
		TransformManager.Instance.AddTransform(card.gameObject, rotationTransform);
	}	
	
	public void ReturnDuelVictoryCardToSlot(Card card, bool isCardChangeSlotBeforeAttack, bool isAttackCard, ActionDelegate onFinish)
	{		
		Transform oldParent = card.transform.parent;		
		card.transform.parent = null;
		Vector3 oldPosition = card.transform.position;
		Quaternion oldRotation = card.transform.rotation;
		card.transform.parent = oldParent;
		
		MoveCardIntoThisSlotImmediately(card);
		card.transform.parent = null;		
		Vector3 targetPosition = card.transform.position;
		
		card.transform.position = oldPosition;
		card.transform.rotation = oldRotation;
		Vector3 subPoint = targetPosition + (targetPosition-oldPosition)*1f - card.transform.forward*2f;
		
		Vector3 distanceDir = oldPosition-targetPosition;
		float sign = -Mathf.Sign(distanceDir.z);
		
		ActionDelegate FinishAnimation = delegate() {
			MoveCardIntoThisSlotImmediately(card);
			onFinish();
		};
		 
		float animationTime = 1f;
		
		MathFunction2D bezier1 = new BezierIntepolation2D(new Vector2(0f, 0f), new Vector2(0.746506f, -0.01541233f), new Vector2(-0.008380715f, 1.051112f), new Vector2(1f, 1f));
		
		ITransform waitTranform = new ST.Wait(animationTime*0.2f);
		ITransform rotationTransform = new ST.TimeRotationTransform(Axis.X,true, animationTime*0.6f, sign*2*Mathf.PI);
		ITransform positionTransform1 = new ST.LinearPositionTransformParam(subPoint, animationTime*0.5f, false, false, delegate(float t) { return bezier1.GetValue(t).y; });
		ITransform positionTransform2 = new ST.LinearPositionTransformParam(targetPosition, 0.5f*animationTime, false, delegate(float t) { return t; });			
		
		if(isCardChangeSlotBeforeAttack)  {
			GameGuiSlot oldSlot = GameGui.Instance.GetSlotWithCard(card);
			DuelAnimationHelper.ReturnCardToSlot(card, oldSlot, onFinish);
		}
		else{
			if(isAttackCard){
				MainAnimationHelper.RunAnimationsConcurrent(card.gameObject, null, FinishAnimation,
			        new ITransform[] { positionTransform1, positionTransform2 }, new ITransform[] {waitTranform, rotationTransform});
			}else{
				FinishAnimation();
			}
		}
			
	}
	
	public void PutCardIntoThisSlotPerserveTransform(Card card)
	{
		Vector3 tmpPosition = card.transform.position;
		Quaternion tmpRotation = card.transform.rotation;
		
		MoveCardIntoThisSlotImmediately(card);
		
		card.transform.position = tmpPosition;
		card.transform.rotation = tmpRotation;
	}
	
	public void MoveCardIntoThisSlotImmediately(Card card)
	{
		MoveCardIntoThisSlotImmediately(card, Cards.Count);
	}
	
	public void MoveCardIntoThisSlotImmediately(Card card, int position)
	{
		GameGuiSlot oldSlot = GameGui.Instance.GetSlotWithCard(card);
		if(oldSlot != null)
			oldSlot.RemoveCardFromSlot(card);
		
		if(!Cards.Contains(card))
			Cards.Add(card);
		card.transform.rotation = GetNextCardRotation(card.Side);
		card.transform.position = GetCardPosition(position);
		
		card.transform.parent = transform;
		
		if(IsCementarySlot)
			card.Reinitialize();		
	}
	
	public void ReleaseCardFromSlot(Card card)
	{
		if(Cards.Contains(card))
			Cards.Remove(card);
	}
	
	public void GetNextCardTransfom(Card card, out Vector3 position, out Quaternion rotation)
	{
		position = GetNextCardPosition();
		rotation = GetNextCardRotation(card.Side);
	}
	
	public Vector3 GetNextCardPosition()
	{
		return GetCardPosition(Cards.Count);
	}
	
	public Vector3 GetCardPosition(int cardNumber)
	{
		Vector3 position = transform.position;
		if(IsPerspective)
			position += (FirstCardDistancePerspective+DistanceBetweenCards*cardNumber)*transform.up;
		else
			position += new Vector3(0.0f, FirstCardDistancePerspective+DistanceBetweenCards*cardNumber, 0.0f);
		return position;		
	}
	
	public Quaternion GetNextCardRotation(CardSide side)
	{
		GameObject tmpObj = new GameObject("tmpObj");
		
		if(IsPerspective)
		{
			if(side == CardSide.Front)
				tmpObj.transform.up = transform.forward;
			else
				tmpObj.transform.LookAt(tmpObj.transform.position + transform.up, transform.forward);
		}
		else
		{
			if(side == CardSide.Front)
				tmpObj.transform.up = GameGui.Instance.GameCamera.transform.up;
			else
				tmpObj.transform.up = -GameGui.Instance.GameCamera.transform.up;
		}			
		
		Quaternion result = tmpObj.transform.rotation;
		Destroy(tmpObj);
		return result;
	}	
	
	public void SetHighlightImmediately(bool enabled)
	{
		if(renderer != null)
			renderer.sharedMaterial.color = (enabled ? Color.red : Color.white);
		else
		{
			Transform marker = transform.FindChild("Marker");
			if(marker != null)
				marker.renderer.enabled = enabled;
		}
	}
	
	public void SetHighlighModeImmediately(HighlightState highlight)
	{
		CancelCurrentTransforms();
		
		switch(highlight)
		{
		case HighlightState.None:
			SetAlphaImmediately(Glow, 0.0f);
			SetAlphaImmediately(Diagram, DiagramNormalAlpa);
			break;
		case HighlightState.Active:
			SetAlphaImmediately(Glow, 1.0f);
			SetAlphaImmediately(Diagram, DiagramNormalAlpa);
			break;
		case HighlightState.Choosen:
			SetAlphaImmediately(Glow, 1.0f);
			SetAlphaImmediately(Diagram, DiagramGlowAlpha);
			break;
		}
	}
	
	public void SetHighlighMode(HighlightState highlight)
	{
		CancelCurrentTransforms();
		if(highlight == HighlightState.Choosen)
			SoundManager.Instance.PlaySound("sfx_slot_aktyw_ID18");
		
		switch(highlight)
		{
		case HighlightState.None:
			SetAlpha(Glow, 0.0f);
			SetAlpha(Diagram, DiagramNormalAlpa);
			break;
		case HighlightState.Active:
			SetAlpha(Glow, 1.0f);
			SetAlpha(Diagram, DiagramNormalAlpa);
			break;
		case HighlightState.Choosen:
			SetAlpha(Glow, 1.0f);
			SetAlpha(Diagram, DiagramGlowAlpha);
			break;
		}		
	}
	
	public void ResetAllCardsPosition()
	{
		GameManager game = GameObject.FindObjectOfType(typeof(GameManager)) as GameManager;
		SlotID slotID = GameGui.Instance.GetSlotID(this);
		List<int> cards = null;
		
		switch(slotID)
		{
		case SlotID.Player1_Cementary: 
			cards = game.Table.Players[0].CardsCementary;
			break;
		case SlotID.Player2_Cementary: 
			cards = game.Table.Players[1].CardsCementary;
			break;	
		case SlotID.Player1_Deck:
			cards = game.Table.Players[0].CardsStack;
			break;
		case SlotID.Player2_Deck: 
			cards = game.Table.Players[1].CardsStack;
			break;		
		default:
			return;
		}
		
		Cards.Clear();
		for(int i = 0; i < cards.Count; ++i)
			MoveCardIntoThisSlotImmediately(GameGui.Instance.GetGraphicForCard(cards[i]));
	}
	
	public void ResetRequireGraphic()
	{
		if(!IsStackSlot && !IsCementarySlot)
			return;
		
		int visibleCount = (IsStackSlot ? 0 : 2);
		
		for(int i = 0; i < Cards.Count; ++i)
			Cards[i].RequireFrontTexture = (i >= Cards.Count-visibleCount);
	}
	
	#endregion
	
	#region Helper methods
	
	private void RemoveCardFromSlot(Card card)
	{
		card.transform.parent = null;
		int index = Cards.IndexOf(card);
		if(index >= 0)
		{
			Cards.RemoveAt(index);
			ResetAllCardsPosition();
		}
	}	
	
	private void SetAlphaImmediately(tk2dSprite sprite, float targetAlpha)
	{
		if(sprite == null)
			return;
		sprite.color = new Color(1.0f, 1.0f, 1.0f, targetAlpha);
	}
	
	private void SetAlpha(tk2dSprite sprite, float targetAlpha)
	{
		if(sprite == null)
			return;
		
		float a = sprite.color.a;
		float velocity = 3.0f;
		float time = Mathf.Abs(a-targetAlpha)/velocity;
		ITransform colorTransform = new ST.ToolkitColorTransform(new Color(1.0f, 1.0f, 1.0f, targetAlpha), time, false);
		
		transformsID.Add(TransformManager.Instance.AddTransform(sprite.gameObject, colorTransform));
	}
	
	private void CancelCurrentTransforms()
	{
		for(int i = 0; i < transformsID.Count; ++i)
			TransformManager.Instance.Cancel(transformsID[i]);
		
		transformsID.Clear();
	}
	
	#endregion
	
}
