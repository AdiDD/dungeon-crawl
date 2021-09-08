﻿using Assets.Source;
using Assets.Source.Core;
using DungeonCrawl.Actors.Static;
using DungeonCrawl.Actors.Static.Environments;
using DungeonCrawl.Actors.Static.Items;
using DungeonCrawl.Actors.Static.Items.Consumables;
using DungeonCrawl.Core;
using System.Text;
using UnityEngine;

namespace DungeonCrawl.Actors.Characters
{
    public class Player : Character
    {
        private CameraController _camera;
        private Inventory _inventory = new Inventory();
        public override int Health { get; protected set; } = 20;
        public override int BaseDamage { get;} = 5;

        private int DamageModifier;
        private int DamageReduction;

        private void Start()
        {
            _camera = CameraController.Singleton;
            _camera.Position = this.Position;
            //_camera.Size -= 2;
        }

        protected override void OnUpdate(float deltaTime)
        {
            UpdateStats();
            DisplayStats();

            if (Input.GetKeyDown(KeyCode.W))
                TryMove(Direction.Up);

            if (Input.GetKeyDown(KeyCode.S))
                TryMove(Direction.Down);

            if (Input.GetKeyDown(KeyCode.A))
                TryMove(Direction.Left);

            if (Input.GetKeyDown(KeyCode.D))
                TryMove(Direction.Right);

            if (Input.GetKeyDown(KeyCode.E))
                PickUp();

            if (Input.GetKeyDown(KeyCode.Space))
                AttemptOpenGate();

            if (Input.GetKeyDown(KeyCode.Q))
                AttemptHeal();
        }

        public override bool OnCollision(Actor anotherActor) => false;

        public override void TryMove(Direction direction)
        {
            var vector = direction.ToVector();
            (int x, int y) targetPosition = (Position.x + vector.x, Position.y + vector.y);

            var actorAtTargetPosition = ActorManager.Singleton.GetActorAt(targetPosition);

            if (actorAtTargetPosition == null)
            {
                UserInterface.Singleton.RemoveText(UserInterface.TextPosition.BottomRight);
                // No obstacle found, just move
                Position = targetPosition;
                _camera.Position = this.Position;
            }
            else
            {
                if (actorAtTargetPosition.OnCollision(this))
                {
                    if (((StaticActor)actorAtTargetPosition).CanPickUp)
                        UserInterface.Singleton.SetText("Press E to pick up", UserInterface.TextPosition.BottomRight);
                    // Allowed to move
                    Position = targetPosition;
                    _camera.Position = this.Position;
                }
                else
                {
                    if (actorAtTargetPosition is Character enemy)
                    {
                        Attack(enemy);
                    }
                }
            }
        }

        protected override void Attack(Character enemy)
        {
            enemy.ApplyDamage(BaseDamage + DamageModifier);

            if(enemy.Health > 0)
                ApplyDamage(enemy.BaseDamage - DamageReduction);
        }

        protected override void OnDeath()
        {
            Debug.Log("Oh no, I'm dead!");
        }

        private void PickUp()
        {
            var item = ActorManager.Singleton.GetActorAt<StaticActor>(Position);

            if (item != null && item.CanPickUp)
            {
                UserInterface.Singleton.RemoveText(UserInterface.TextPosition.BottomRight);
                _inventory.AddItem(item);
                //ActorManager.Singleton.DestroyActor(item);
                item.Position = (-5, 1);
            }
        }

        private void AttemptOpenGate()
        {
            AdjecentCoordinates adjecentCoordinatesCreator = new AdjecentCoordinates(Position);
            var adjecentCoordinates = adjecentCoordinatesCreator.GetAdjecentCoordinates();

            for (int i = 0; i < 4; i++)
            {
                var nextCell = ActorManager.Singleton.GetActorAt<Actor>(adjecentCoordinates[i]);

                if (nextCell != null && nextCell is LockedGate lockedGate)
                {
                    if (_inventory.HasKey())
                    {
                        _inventory.RemoveKey();
                        lockedGate.OpenGate();
                    }
                    else
                    {
                        UserInterface.Singleton.SetText("Need a Key!", UserInterface.TextPosition.BottomRight);
                    }
                }
            }
        }

        private void AttemptHeal()
        {
            var healthKit = (HealthKit)_inventory.GetItem("HealthKit");

            if (healthKit != null)
            {
                Health += healthKit.Heal;
                _inventory.RemoveItem(healthKit);
                UserInterface.Singleton.SetText("Das gud! MEIN LEBEN!", UserInterface.TextPosition.BottomRight);
            }
            else
            {
                UserInterface.Singleton.SetText("Nein! Ich haben nicht!", UserInterface.TextPosition.BottomRight);
            }
        }

        public void UpdateStats()
        {
            DamageModifier = _inventory.AttackPower;
            DamageReduction = _inventory.Defense;
        }

        public void DisplayStats()
        {
            StringBuilder sb = new StringBuilder($"{DefaultName} :\n\n");
            sb.Append($"Health: {Health}\n");
            sb.Append($"Attack Power: {BaseDamage + DamageModifier}\n");
            sb.Append($"Defense: {DamageReduction}\n");
            UserInterface.Singleton.SetText(sb.ToString(), UserInterface.TextPosition.TopRight);

            UserInterface.Singleton.SetText(_inventory.ToString(), UserInterface.TextPosition.TopLeft);
        }

        public override int DefaultSpriteId => 24;
        public override string DefaultName => "Player";
    }
}
