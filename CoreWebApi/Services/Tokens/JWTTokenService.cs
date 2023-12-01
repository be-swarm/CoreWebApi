using Microsoft.IdentityModel.Tokens;
using BeSwarm.WebApi.Core;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BeSwarm.CoreWebApi.Services.Tokens
{
	public class JWTTokenService : ITokenService
	{
		private TokenRSAKeys _rsakeys;
		public JWTTokenService(TokenRSAKeys rsakeys)
		{
			_rsakeys = rsakeys;
		}
		public async Task<ResultAction<TokenResult>> CreateToken(Dictionary<string, string> claims,int ttl)
		{
			ResultAction<TokenResult> ret = new();
			TokenRSAKey rsakey = _rsakeys.GetCurrentPrivateKey();
			if (rsakey is null)
			{
				ret.SetError(new InternalError("No private key set in rsakeys"), StatusAction.internalerror);
				return ret;
			}

			//Set issued at date
			DateTime issuedAt = DateTime.UtcNow;
			//set the time when it expires
			DateTime expires = DateTime.UtcNow.AddSeconds(ttl);
			var rsaparams = new RSACryptoServiceProvider();
			rsaparams.FromXmlString(rsakey.key);
			var rsasecuritykey = new RsaSecurityKey(rsaparams);
			var signinCredentials = new SigningCredentials(rsasecuritykey, SecurityAlgorithms.RsaSha256Signature);
			List<Claim> lclaims = new();
			foreach (var claim in claims)
			{
				lclaims.Add(new Claim(claim.Key, claim.Value));
			};
	
			var token = new JwtSecurityToken(
				claims: lclaims,
				expires: expires,
				notBefore: issuedAt,
				signingCredentials: signinCredentials
			);
			token.Header.Add("kid", rsakey.indice);
			var tokenresult = new JwtSecurityTokenHandler().WriteToken(token);
	
			if (ret.IsOk)
			{
				ret.datas.token_type = "Bearer";
				ret.datas.id_token = tokenresult;
				ret.datas.expires_in = ttl.ToString();
				ret.datas.expires_on = ((int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1).AddSeconds(ttl))).TotalSeconds).ToString();
			}
			return ret;
			
		}

		public async Task<ResultAction<Dictionary<string, string>>> GetToken(string? token)
		{
			ResultAction<Dictionary<string, string>> ret = new();
			if (token is null)
			{
				ret.SetError(new InternalError("Token is null"), StatusAction.forbidden);
				return ret;
			}
			if (!token.StartsWith("Bearer "))
			{
			   ret.SetError(new InternalError("Invalid bearer token"), StatusAction.forbidden);
			   return ret;			 
			}
			try
			{   // get kid
				var jwtHandler = new JwtSecurityTokenHandler();
				var jwtOutput = string.Empty;
				string stoken = token.Substring(7);
				// Check Token Format
				if (!jwtHandler.CanReadToken(stoken))
				{
					ret.SetError(new InternalError("Invalid JWT string token"), StatusAction.forbidden);
					return ret;
				}
				var tokenjwt = jwtHandler.ReadJwtToken(stoken);
				var header = tokenjwt.Header;
				string kid = header.Kid;
				// find public key
				string pubkey = _rsakeys.GetPublicKey(kid);
				if (pubkey == null)
				{
					ret.SetError(new InternalError("unknonw kid JWT token"), StatusAction.forbidden);
					return ret;
				}
				var rsaparamsverif = new RSACryptoServiceProvider();
				rsaparamsverif.FromXmlString(pubkey);
				SecurityToken securityKey = new JwtSecurityToken();
				var validtoken = new TokenValidationParameters()
				{
					ValidateAudience = false,
					ValidateLifetime = false,
					ValidateIssuer = false,
					IssuerSigningKey = new RsaSecurityKey(rsaparamsverif)
				};
				var jwthandler = new JwtSecurityTokenHandler();
				var claims = jwthandler.ValidateToken(stoken, validtoken, out securityKey);

				// token is expired ?
				long expires = long.Parse(claims.Claims.Single(x => x.Type == "exp").Value) - (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
				if (expires < 0)
				{
					ret.SetError(new InternalError("token is expired", -1), StatusAction.unauthorized);
					return ret;
				}

				foreach (var claiml in claims.Claims)
				{
					ret.datas.Add(claiml.Type, claiml.Value);
				}
			}
			catch (SecurityTokenValidationException e)
			{
				ret.SetError(new InternalError(e.Message), StatusAction.forbidden);
			}
			catch (Exception ex)
			{
				ret.SetError(new InternalError("Invalid JWT string token"), StatusAction.forbidden);
			}



			return ret;
		}
	}
}
