﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using System.Security.Cryptography;
using GTA.Math;
using GTA;

namespace RageCoop.Server
{
    /// <summary>
    /// Represents an prop owned by server.
    /// </summary>
    public class ServerProp
    {
        private Server Server;
        internal ServerProp(Server server)
        {
            Server= server;
        }


        /// <summary>
        /// Pass the value as an argument in <see cref="Scripting.API.SendCustomEvent(int, List{object}, List{Client})"/> or <see cref="Client.SendCustomEvent(int, object[])"/> to convert this object to handle at client side.
        /// </summary>
        public Tuple<byte,byte[]> Handle
        {
            get
            {
                return new(50, BitConverter.GetBytes(ID));
            }
        }

        /// <summary>
        /// Delete this prop
        /// </summary>
        public void Delete()
        {
            Server.API.SendCustomEvent(CustomEvents.DeleteServerProp, new() { ID });
        }

        /// <summary>
        /// Network ID of this object.
        /// </summary>
        public int ID { get; internal set; }

        /// <summary>
        /// The object's model
        /// </summary>
        public Model Model { get; internal set; }

        /// <summary>
        /// Gets or sets this object's position
        /// </summary>
        public Vector3 Position 
        { 
            get { return _pos; } 
            set { _pos=value; Server.BaseScript.SendServerObjectsTo(new() { this }); } 
        }
        private Vector3 _pos;

        /// <summary>
        /// Gets or sets this object's rotation
        /// </summary>
        public Vector3 Rotation
        {
            get { return _rot; }
            set { _rot=value; Server.BaseScript.SendServerObjectsTo(new() { this }); }
        }
        private Vector3 _rot;
    }
    /// <summary>
    /// Represents a ped from a client
    /// </summary>
    public class ServerPed
    {
        internal ServerPed()
        {

        }

        /// <summary>
        /// Pass the value as an argument in <see cref="Scripting.API.SendCustomEvent(int, List{object}, List{Client})"/> or <see cref="Client.SendCustomEvent(int, object[])"/> to convert this object to handle at client side.
        /// </summary>
        /// <returns></returns>
        public Tuple<byte, byte[]> Handle
        {
            get
            {
                return new(51, BitConverter.GetBytes(ID));
            }
        }

        /// <summary>
        /// The <see cref="Client"/> that is responsible synchronizing for this ped.
        /// </summary>
        public Client Owner { get; internal set; }

        /// <summary>
        /// The ped's network ID (not handle!).
        /// </summary>
        public int ID { get; internal set; }

        /// <summary>
        /// Whether this ped is a player.
        /// </summary>
        public bool IsPlayer { get { return Owner?.Player==this; } }

        /// <summary>
        /// The ped's last vehicle.
        /// </summary>
        public ServerVehicle LastVehicle { get; internal set; }

        /// <summary>
        /// Position of this ped
        /// </summary>
        public Vector3 Position { get; internal set; }


        /// <summary>
        /// Gets or sets this ped's rotation
        /// </summary>
        public Vector3 Rotation { get; internal set; }

        /// <summary>
        /// Health
        /// </summary>
        public int Health { get; internal set; }
    }
    /// <summary>
    /// Represents a vehicle from a client
    /// </summary>
    public class ServerVehicle
    {
        internal ServerVehicle()
        {

        }

        /// <summary>
        /// Pass the value as an argument in <see cref="Scripting.API.SendCustomEvent(int, List{object}, List{Client})"/> or <see cref="Client.SendCustomEvent(int, object[])"/> to convert this object to handle at client side.
        /// </summary>
        /// <returns></returns>
        public Tuple<byte, byte[]> Handle
        {
            get{
                return new(52, BitConverter.GetBytes(ID));
            }
        }

        /// <summary>
        /// The <see cref="Client"/> that is responsible synchronizing for this vehicle.
        /// </summary>
        public Client Owner { get; internal set; }

        /// <summary>
        /// The vehicle's network ID (not handle!).
        /// </summary>
        public int ID { get; internal set; }

        /// <summary>
        /// Position of this vehicle
        /// </summary>
        public Vector3 Position { get; internal set; }

        /// <summary>
        /// Gets or sets this vehicle's quaternion
        /// </summary>
        public Quaternion Quaternion { get; internal set; }
    }

    /// <summary>
    /// Manipulate entities from the server
    /// </summary>
    public class ServerEntities
    {
        private readonly Server Server;
        internal ServerEntities(Server server)
        {
            Server = server;
        }
        internal Dictionary<int, ServerPed> Peds { get; set; } = new();
        internal Dictionary<int, ServerVehicle> Vehicles { get; set; } = new();
        internal Dictionary<int,ServerProp> ServerProps { get; set; }=new();
        
        /// <summary>
        /// Get a <see cref="ServerPed"/> by it's id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ServerPed GetPedByID(int id)
        {
            if(Peds.TryGetValue(id,out var ped))
            {
                return ped;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get a <see cref="ServerVehicle"/> by it's id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ServerVehicle GetVehicleByID(int id)
        {
            if (Vehicles.TryGetValue(id, out var veh))
            {
                return veh;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get a <see cref="ServerProp"/> owned by server from it's ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ServerProp GetPropByID(int id)
        {
            if (ServerProps.TryGetValue(id, out var obj))
            {
                return obj;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// Create a static prop owned by server.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <returns></returns>
        public ServerProp CreateProp(Model model,Vector3 pos,Vector3 rot)
        {
            int id = RequestID();
            ServerProp prop;
            ServerProps.Add(id,prop=new ServerProp(Server)
            {
                ID=id,
                Model=model,
                Position=pos,
                Rotation=rot
            });
            return prop;
        }

        /// <summary>
        /// Get all peds on this server
        /// </summary>
        /// <returns></returns>
        public ServerPed[] GetAllPeds()
        {
            return Peds.Values.ToArray();
        }

        /// <summary>
        /// Get all vehicles on this server
        /// </summary>
        /// <returns></returns>
        public ServerVehicle[] GetAllVehicle()
        {
            return Vehicles.Values.ToArray();
        }

        /// <summary>
        /// Get all static objects owned by server
        /// </summary>
        /// <returns></returns>
        public ServerProp[] GetAllProps()
        {
            return ServerProps.Values.ToArray();
        } 

        /// <summary>
        /// Not thread safe
        /// </summary>
        internal void Update(Packets.PedSync p,Client sender)
        {
            ServerPed ped;
            if(!Peds.TryGetValue(p.ID,out ped))
            {
                Peds.Add(p.ID,ped=new ServerPed());
                ped.ID=p.ID;
            }
            ped.Position = p.Position;
            ped.Owner=sender;
            ped.Health=p.Health;
            ped.Rotation=p.Rotation;
            ped.Owner=sender;
        }
        internal void Update(Packets.VehicleSync p, Client sender)
        {
            ServerVehicle veh;
            if (!Vehicles.TryGetValue(p.ID, out veh))
            {
                Vehicles.Add(p.ID, veh=new ServerVehicle());
                veh.ID=p.ID;
            }
            veh.Position = p.Position;
            veh.Owner=sender;
            veh.Quaternion=p.Quaternion;
        }
        internal void Update(Packets.VehicleStateSync p, Client sender)
        {
            ServerVehicle veh;
            if (!Vehicles.TryGetValue(p.ID, out veh))
            {
                Vehicles.Add(p.ID, veh=new ServerVehicle());
                veh.ID=p.ID;
            }
            foreach(var pair in p.Passengers)
            {
                if(Peds.TryGetValue(pair.Value,out var ped))
                {
                    ped.LastVehicle=veh;
                }
            }
        }
        internal void CleanUp(Client left)
        {
            Server.Logger?.Trace("Removing all entities from: "+left.Username);

            foreach (var pair in Peds)
            {
                if (pair.Value.Owner==left)
                {
                    Server.QueueJob(()=>Peds.Remove(pair.Key));
                }
            }
            foreach (var pair in Vehicles)
            {
                if (pair.Value.Owner==left)
                {
                    Server.QueueJob(() => Vehicles.Remove(pair.Key));
                }
            }
            Server.QueueJob(() =>
            Server.Logger?.Trace("Remaining entities: "+(Peds.Count+Vehicles.Count+ServerProps.Count)));
        }
        internal void RemoveVehicle(int id)
        {
            // Server.Logger?.Trace($"Removing vehicle:{id}");
            if (Vehicles.ContainsKey(id)) { Vehicles.Remove(id); }
        }
        internal void RemovePed(int id)
        {
            // Server.Logger?.Trace($"Removing ped:{id}");
            if (Peds.ContainsKey(id)) { Peds.Remove(id); }
        }

        internal void Add(ServerPed ped)
        {
            if (Peds.ContainsKey(ped.ID))
            {
                Peds[ped.ID]=ped;
            }
            else
            {
                Peds.Add(ped.ID, ped);
            }
        }
        internal int RequestID()
        {
            int ID = 0;
            while ((ID==0)
                || ServerProps.ContainsKey(ID)
                || Peds.ContainsKey(ID)
                || Vehicles.ContainsKey(ID))
            {
                byte[] rngBytes = new byte[4];

                RandomNumberGenerator.Create().GetBytes(rngBytes);

                // Convert the bytes into an integer
                ID = BitConverter.ToInt32(rngBytes, 0);
            }
            return ID;
        }
    }
}
