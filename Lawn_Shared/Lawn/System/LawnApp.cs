﻿using System;
using System.Collections.Generic;
using System.Threading;
//using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Media;
using Sexy;
using Sexy.TodLib;

namespace Lawn
{
	public/*internal*/ class LawnApp : SexyAppBase
	{
		public PlayerInfo mPlayerInfo
		{
			get
			{
				return _playerInfo;
			}
			set
			{
				_playerInfo = value;
				if (mPlayerInfo != null)
				{
					SetMusicVolume(mPlayerInfo.mMusicVolume);
					SetSfxVolume(mPlayerInfo.mSoundVolume);
				}
			}
		}

		public static void CenterDialog(Dialog theDialog, int theWidth, int theHeight)
		{
			theDialog.Resize((Constants.BOARD_WIDTH - theWidth) / 2, (Constants.BOARD_HEIGHT - theHeight) / 2, theWidth, theHeight);
		}

		public LawnApp(Main m) : base(m)
		{
			mBoard = null;
			mGameSelector = null;
			mSeedChooserScreen = null;
			mAwardScreen = null;
			mCreditScreen = null;
			mTitleScreen = null;
			mSoundSystem = null;
			mKonamiCheck = null;
			mMustacheCheck = null;
			mMoustacheCheck = null;
			mSuperMowerCheck = null;
			mSuperMowerCheck2 = null;
			mFutureCheck = null;
			mPinataCheck = null;
			mDanceCheck = null;
			mDaisyCheck = null;
			mSukhbirCheck = null;
			mMustacheMode = false;
			mSuperMowerMode = false;
			mFutureMode = false;
			mPinataMode = false;
			mDanceMode = false;
			mDaisyMode = false;
			mSukhbirMode = false;
			mGameScene = GameScenes.SCENE_LOADING;
			mZenGarden = null;
			mEffectSystem = null;
			mReanimatorCache = null;
			mCloseRequest = false;
			mWidth = Constants.BOARD_WIDTH;
			mHeight = Constants.BOARD_HEIGHT;
			mAppCounter = 0;
			mAppRandSeed = DateTime.UtcNow.Millisecond;
			mTrialType = TrialType.TRIAL_NONE;
			mDebugTrialLocked = false;
			mMuteSoundsForCutscene = false;
			base.mMusicVolume = 0.85;
			mSfxVolume = 0.85;
			mAutoStartLoadingThread = false;
			mProdName = "PlantsVsZombies";
			string aTitle = "Plants vs. Zombies";
			mTitle = aTitle;
			mPlayerInfo = null;
			mLastLevelStats = new LevelStats();
			mFirstTimeGameSelector = true;
			mGameMode = GameMode.GAMEMODE_ADVENTURE;
			mEasyPlantingCheat = false;
			mLoadingZombiesThreadCompleted = true;
			mGamesPlayed = 0;
			mMaxExecutions = 0;
			mMaxPlays = 0;
			mMaxTime = 0;
			mCompletedLoadingThreadTasks = 0;
			mProfileMgr = new ProfileMgr();
			mRegisterResourcesLoaded = false;
			mTodCheatKeys = false;
			mCrazyDaveReanimID = null;
			mCrazyDaveState = CrazyDaveState.CRAZY_DAVE_OFF;
			mCrazyDaveBlinkCounter = 0;
			mCrazyDaveBlinkReanimID = null;
			mCrazyDaveMessageIndex = -1;
			mLawnMessageBoxListener = null;
			ReportAchievement.AchievementsChanged += ReportAchievement_AchievementsChanged;
		}

		private void ReportAchievement_AchievementsChanged()
		{
			if (mPlayerInfo != null)
			{
				mPlayerInfo.UpdateAchievementInfo();
			}
		}

		public override void Dispose()
		{
			if (mBoard != null)
			{
				WriteCurrentUserConfig();
			}
			if (mBoard != null)
			{
				mBoardResult = BoardResult.BOARDRESULT_QUIT_APP;
				mBoard.TryToSaveGame();
				mWidgetManager.RemoveWidget(mBoard);
				mBoard.Dispose();
				mBoard = null;
			}
			if (mTitleScreen != null)
			{
				mWidgetManager.RemoveWidget(mTitleScreen);
				mTitleScreen.Dispose();
			}
			if (mGameSelector != null)
			{
				mWidgetManager.RemoveWidget(mGameSelector);
				mGameSelector.Dispose();
			}
			if (mSeedChooserScreen != null)
			{
				mWidgetManager.RemoveWidget(mSeedChooserScreen);
				mSeedChooserScreen.Dispose();
			}
			if (mAwardScreen != null)
			{
				mWidgetManager.RemoveWidget(mAwardScreen);
				mAwardScreen.Dispose();
			}
			if (mCreditScreen != null)
			{
				mWidgetManager.RemoveWidget(mCreditScreen);
				mCreditScreen.Dispose();
			}
			mProfileMgr.Dispose();
			mResourceManager.DeleteResources("");
		}

		public bool KillNewOptionsDialog()
		{
			if ((NewOptionsDialog)base.GetDialog(Dialogs.DIALOG_NEWOPTIONS) == null)
			{
				return false;
			}
			KillDialog(2);
			return true;
		}

		public bool KillLeaderboardDialog()
		{
			if ((LeaderboardDialog)base.GetDialog(Dialogs.DIALOG_LEADERBOARD) == null)
			{
				return false;
			}
			KillDialog(59);
			mLeaderboardScreen.SetGrayed(false);
			return true;
		}

		public override void GotFocus()
		{
			if (mSoundManager != null)
			{
				mSoundManager.Enable(true);
			}
			if (mMusicInterface != null)
			{
				mMusicInterface.Enable(mMusicEnabled);
			}
			if (mCreditScreen != null)
			{
				mCreditScreen.AppGotFocus();
			}
			if (checkGiveAchievements && !SexyAppBase.IsInTrialMode)
			{
				checkGiveAchievements = false;
				ReportAchievement.GiveAchievement(achievementToCheck);
			}
			base.GotFocus();
		}

		public override void LostFocus()
		{
			base.LostFocus();
			if (mSoundManager != null)
			{
				mSoundManager.StopAllSounds();
			}
			if (mBoard != null && mBoard.mBoardFadeOutCounter > 0)
			{
				mBoard.mBoardFadeOutCounter = 3;
			}
			if (!mTodCheatKeys && CanPauseNow())
			{
				if (mBoard != null)
				{
					mBoard.RefreshSeedPacketFromCursor();
				}
				DoPauseDialog();
			}
		}

		public override void AppEnteredBackground()
		{
			WriteRestoreInfo();
			if (mBoard != null)
			{
				mBoard.TryToSaveGame();
			}
			WriteCurrentUserConfig();
		}

		public override void InitHook()
		{
		}

		public override void WriteToRegistry()
		{
			if (mPlayerInfo != null)
			{
				base.RegistryWriteString("CurUser", mPlayerInfo.mName);
				mPlayerInfo.SaveDetails();
			}
			base.WriteToRegistry();
		}

		public override void LoadingThreadProc()
		{
			GameConstants.Init();
			if (!TodCommon.TodLoadResources("LoaderBar") || !TodCommon.TodLoadResources("LoaderBarFont"))
			{
				return;
			}
			Resources.ExtractLoaderBarFontResources(mResourceManager);
			Resources.ExtractLoaderBarResources(mResourceManager);
			AtlasResources.mAtlasResources.UnpackLoadingAtlasImages();
			Resources.LinkUpResArray();
			ReanimationParams[] array = new ReanimationParams[]
			{
				new ReanimationParams(ReanimationType.REANIM_LOADBAR_SPROUT, "reanim/loadbar_sprout", 1),
				new ReanimationParams(ReanimationType.REANIM_LOADBAR_ZOMBIEHEAD, "reanim/loadbar_zombiehead", 1)
			};
			ReanimatorXnaHelpers.ReanimatorLoadDefinitions(ref array, array.Length);
			TodStringFile.TodStringListLoad("Content/"+"LawnStrings_" + Constants.LanguageSubDir + ".txt");
			mTitleScreen.mLoaderScreenIsLoaded = true;
			mNumLoadingThreadTasks += mResourceManager.GetNumResources("LoadingFonts") * 54;
			mNumLoadingThreadTasks += mResourceManager.GetNumResources("LoadingImages") * 9;
			mNumLoadingThreadTasks += mResourceManager.GetNumResources("LoadingSounds") * 54;
			mNumLoadingThreadTasks += 612;
			mNumLoadingThreadTasks += 8092;
			mNumLoadingThreadTasks += 360500;
			mNumLoadingThreadTasks += GetNumPreloadingTasks();
			mNumLoadingThreadTasks += mMusic.GetNumLoadingTasks();
			if (!Main.LOW_MEMORY_DEVICE)
			{
				DelayLoadGamePlayResources(true);
				DelayLoadLeaderboardResource(true);
				DelayLoadCachedResources(true);
				DelayLoadZenGardenResources(true);
			}
			DelayLoadMainMenuResource(true);
			mResourceManager.LoadAllResources();
			Resources.ExtractResources(mResourceManager, AtlasResources.mAtlasResources);
			AtlasResources.mAtlasResources.ExtractResources();
			ReanimatorXnaHelpers.ReanimatorLoadDefinitions(ref GameConstants.gLawnReanimationArray, 119);
			TodStringFile.TodStringListSetColors(GameConstants.gLawnStringFormats, GameConstants.gLawnStringFormatCount);
			if (mLoadingFailed || mShutdown || mCloseRequest)
			{
				return;
			}
			mMusic.MusicInit();
			mZenGarden = new ZenGarden();
			mReanimatorCache = new ReanimatorCache();
			mReanimatorCache.ReanimatorCacheInitialize();
			TodFoley.TodFoleyInitialize(null, 103);
			GlobalMembersTrail.TrailLoadDefinitions(GameConstants.gLawnTrailArray, 1);
			TodParticleGlobal.TodParticleLoadDefinitions(ref GameConstants.gLawnParticleArray, 102);
			PreloadForUser();
			if (!mLoadingFailed && !mShutdown)
			{
				bool flag = mCloseRequest;
			}
			// init filter effects
			for (int i = 0; i < (int)FilterEffectType.NUM_FILTER_EFFECTS; i++)
			{
				FilterEffect.FilterEffectInitTexture(AtlasResources.IMAGE_REANIM_IMITATER_BLINK1.Texture, (FilterEffectType)i);
			}
		}

		public virtual void LoadingCompleted()
		{
			mWidgetManager.RemoveWidget(mTitleScreen);
			base.SafeDeleteWidget(mTitleScreen);
			mTitleScreen = null;
			mResourceManager.DeleteImage("IMAGE_TITLESCREEN");
			if (mRestoreLocation == RestoreLocation.RESTORE_BOARD && RestoreGame())
			{
				return;
			}
			ShowGameSelector();
			if (Main.LOW_MEMORY_DEVICE)
			{
				mResourceManager.UnloadInitResources();
			}
		}

		public static void PreallocateMemory()
		{
			GC.Collect();
			ReanimatorTransform.PreallocateMemory();
			Reanimation.PreallocateMemory();
			ReanimatorTrackInstance.PreallocateMemory();
			Attachment.PreallocateMemory();
			Plant.PreallocateMemory();
			Zombie.PreallocateMemory();
			Projectile.PreallocateMemory();
			XNASoundInstance.PreallocateMemory();
			TodParticle.PreallocateMemory();
			RenderItem.PreallocateMemory();
			TodParticleEmitter.PreallocateMemory();
			SexyAppBase.XnaGame.CompensateForSlowUpdate();
		}

		public override void LoadingThreadCompleted()
		{
			PropertiesParser propertiesParser = new PropertiesParser(this);
			if (propertiesParser.ParsePropertiesFile("properties/content.xml"))
			{
				string @string = base.GetString("ContentUpdateSitePrefix");
				if (!string.IsNullOrEmpty(@string))
				{
					Debug.OutputDebug<string>(Common.StrFormat_("Content Update: URL={0}\n", @string));
				}
				else
				{
					Debug.OutputDebug<string>("Content Update: Failed to find property 'ContentUpdateSitePrefix'.\n");
				}
			}
			else
			{
				Debug.OutputDebug<string>(Common.StrFormat_("Content Update: Failed to parse properties file: {0}\n", propertiesParser.GetErrorText()));
			}
			
			
			// Cached GameObjects
			for (SeedType i = 0; i < SeedType.NUM_SEED_TYPES; i++) 
			{
				if (i == SeedType.SEED_SPROUT) continue;
				mReanimatorCache.MakeCachedPlantFrame(i, DrawVariation.VARIATION_NORMAL);
			}
			for (LawnMowerType i = 0; i < LawnMowerType.NUM_MOWER_TYPES ; i++)
			{
				mReanimatorCache.MakeCachedMowerFrame(i);
			}
			for (ZombieType i = 0; i < ZombieType.NUM_CACHED_ZOMBIE_TYPES; i++)
			{
				if (i == ZombieType.NUM_ZOMBIE_TYPES) continue;
				mReanimatorCache.MakeCachedZombieFrame(i);
			}
			for (DrawVariation j = DrawVariation.VARIATION_MARIGOLD_WHITE; j <= DrawVariation.VARIATION_MARIGOLD_LIGHT_GREEN; j++) 
			{
				mReanimatorCache.MakeCachedPlantFrame(SeedType.SEED_MARIGOLD, j);
			}
			//mReanimatorCache.MakeCachedPlantFrame(SeedType.SEED_MARIGOLD, DrawVariation.VARIATION_MARIGOLD_WHITE);

			GC.Collect();
			SexyAppBase.XnaGame.CompensateForSlowUpdate();
		}

		public virtual bool DebugKeyDown(int theKey)
		{
			return false;
		}

		public virtual void HandleCmdLineParam(string theParamName, string theParamValue)
		{
		}

		public override void PlaySample(int theSoundNum)
		{
			if (!mMuteSoundsForCutscene)
			{
				base.PlaySample(theSoundNum);
			}
		}

		public void ConfirmQuit()
		{
			string theDialogLines = TodStringFile.TodStringTranslate("[QUIT_MESSAGE]");
			string theDialogHeader = TodStringFile.TodStringTranslate("[QUIT_HEADER]");
			LawnDialog lawnDialog = DoDialog(13, true, theDialogHeader, theDialogLines, "", 2);
			lawnDialog.mLawnYesButton.mLabel = TodStringFile.TodStringTranslate("[QUIT_BUTTON]");
			LawnApp.CenterDialog(lawnDialog, lawnDialog.mWidth, lawnDialog.mHeight);
		}

		public void ConfirmCheckForUpdates()
		{
		}

		public void CheckForUpdates()
		{
		}

		public void DoUserDialog()
		{
			KillDialog(29);
			UserDialog userDialog = new UserDialog(this);
			LawnApp.CenterDialog(userDialog, userDialog.mWidth, userDialog.mHeight);
			base.AddDialog(29, userDialog);
			mWidgetManager.SetFocus(userDialog);
		}

		public void FinishUserDialog(bool isYes)
		{
			UserDialog userDialog = (UserDialog)base.GetDialog(Dialogs.DIALOG_USERDIALOG);
			if (userDialog == null)
			{
				return;
			}
			if (isYes)
			{
				PlayerInfo profile = mProfileMgr.GetProfile(userDialog.GetSelName());
				if (profile != null)
				{
					mPlayerInfo = profile;
					mWidgetManager.MarkAllDirty();
					if (mGameSelector != null)
					{
						mGameSelector.SyncProfile(true);
					}
				}
			}
			KillDialog(29);
		}

		public void DoCreateUserDialog(bool isOnlyUser)
		{
			FinishCreateUserDialog(true);
		}

		public void DoCheatDialog()
		{
			KillDialog(35);
			CheatDialog cheatDialog = new CheatDialog(this);
			LawnApp.CenterDialog(cheatDialog, cheatDialog.mWidth, cheatDialog.mHeight);
			base.AddDialog(35, cheatDialog);
		}

		public void FinishCheatDialog(bool isYes)
		{
			CheatDialog cheatDialog = (CheatDialog)base.GetDialog(Dialogs.DIALOG_CHEAT);
			if (cheatDialog == null)
			{
				return;
			}
			if (isYes && !cheatDialog.ApplyCheat())
			{
				return;
			}
			KillDialog(35);
			if (isYes)
			{
				mMusic.StopAllMusic();
				mBoardResult = BoardResult.BOARDRESULT_CHEAT;
				PreNewGame(mGameMode, false);
			}
		}

		public void FinishCreateUserDialog(bool isYes)
		{
			string gamertag = "Player";//Gamer.SignedInGamers[PlayerIndex.One].Gamertag;
			string theDialogLines = "[ENTER_NEW_USER]";
			if (isYes && gamertag.empty() && mPlayerInfo != null)
			{
				KillDialog(30);
				return;
			}
			if (mPlayerInfo == null && (!isYes || gamertag.empty()))
			{
				DoDialog(33, true, "[ENTER_YOUR_NAME]", theDialogLines, "[DIALOG_BUTTON_OK]", 3);
				return;
			}
			if (!isYes)
			{
				KillDialog(30);
				return;
			}
			PlayerInfo playerInfo = mProfileMgr.AddProfile(gamertag);
			if (playerInfo == null)
			{
				DoDialog(33, true, "[NAME_CONFLICT]", "[ENTER_UNIQUE_PLAYER_NAME]", "[DIALOG_BUTTON_OK]", 3);
				return;
			}
			mProfileMgr.Save();
			mPlayerInfo = playerInfo;
			KillDialog(29);
			KillDialog(30);
			mWidgetManager.MarkAllDirty();
			if (mGameSelector != null)
			{
				mGameSelector.SyncProfile(true);
			}
		}

		public void DoConfirmDeleteUserDialog(string theName)
		{
			KillDialog(31);
			DoDialog(31, true, "[ARE_YOU_SURE]", Common.StrFormat_(TodStringFile.TodStringTranslate("[DELETE_USER_WARNING]"), theName), "", 1);
		}

		public void FinishConfirmDeleteUserDialog(bool isYes)
		{
			KillDialog(31);
			UserDialog userDialog = (UserDialog)base.GetDialog(Dialogs.DIALOG_USERDIALOG);
			if (userDialog == null)
			{
				return;
			}
			mWidgetManager.SetFocus(userDialog);
			if (!isYes)
			{
				return;
			}
			string text = (mPlayerInfo != null) ? mPlayerInfo.mName : "";
			string selName = userDialog.GetSelName();
			if (selName == text)
			{
				mPlayerInfo = null;
			}
			mProfileMgr.DeleteProfile(selName);
			userDialog.FinishDeleteUser();
			if (mPlayerInfo == null)
			{
				mPlayerInfo = mProfileMgr.GetProfile(userDialog.GetSelName());
				if (mPlayerInfo == null)
				{
					mPlayerInfo = mProfileMgr.GetAnyProfile();
				}
			}
			mProfileMgr.Save();
			if (mPlayerInfo == null)
			{
				DoCreateUserDialog(true);
			}
			mWidgetManager.MarkAllDirty();
			if (mGameSelector != null)
			{
				mGameSelector.SyncProfile(true);
			}
		}

		public void DoRenameUserDialog(string theName)
		{
			KillDialog(32);
			NewUserDialog newUserDialog = new NewUserDialog(this, true, true);
			newUserDialog.Move(mWidth / 2 - newUserDialog.mWidth / 2, (int)Constants.InvertAndScale(20f));
			newUserDialog.SetName(theName);
			base.AddDialog(32, newUserDialog);
		}

		public void FinishRenameUserDialog(bool isYes)
		{
			UserDialog userDialog = (UserDialog)base.GetDialog(29);
			if (!isYes)
			{
				KillDialog(32);
				mWidgetManager.SetFocus(userDialog);
				return;
			}
			NewUserDialog newUserDialog = (NewUserDialog)base.GetDialog(32);
			if (userDialog == null || newUserDialog == null)
			{
				return;
			}
			string selName = userDialog.GetSelName();
			string name = newUserDialog.GetName();
			if (string.IsNullOrEmpty(name))
			{
				KillDialog(32);
				mWidgetManager.SetFocus(userDialog);
				return;
			}
			bool flag = mProfileMgr.GetProfile(selName) == mPlayerInfo;
			if (!mProfileMgr.RenameProfile(selName, name))
			{
				DoDialog(34, true, "[NAME_CONFLICT]", "[ENTER_UNIQUE_PLAYER_NAME]", "[DIALOG_BUTTON_OK]", 3);
				return;
			}
			mProfileMgr.Save();
			if (flag)
			{
				mPlayerInfo = mProfileMgr.GetProfile(name);
			}
			userDialog.FinishRenameUser(name);
			mWidgetManager.MarkAllDirty();
			KillDialog(32);
			mWidgetManager.SetFocus(userDialog);
		}

		public void FinishNameError(int theId)
		{
			KillDialog(theId);
			NewUserDialog newUserDialog = (NewUserDialog)base.GetDialog(Dialogs.DIALOG_CREATEUSER);
			if (newUserDialog != null)
			{
				mWidgetManager.SetFocus(newUserDialog.mNameEditWidget);
			}
		}

		public void FinishRestartConfirmDialog()
		{
			mKilledYetiAndRestarted = mBoard.mKilledYeti;
			KillDialog(37);
			KillDialog(39);
			KillBoard();
			PreNewGame(mGameMode, false);
		}

		public void FinishInGameRestartConfirmDialog(bool isYes)
		{
			KillDialog(23);
			if (isYes)
			{
				mMusic.StopAllMusic();
				mSoundSystem.CancelPausedFoley();
				KillNewOptionsDialog();
				mBoardResult = BoardResult.BOARDRESULT_RESTART;
				if (mBoard != null)
				{
					mKilledYetiAndRestarted = mBoard.mKilledYeti;
				}
				PreNewGame(mGameMode, false);
			}
		}

		public void FinishAboutDialog(bool isYes)
		{
			KillDialog(52);
		}

		public void FinishRestartWarningDialog(bool isYes)
		{
			KillDialog(53);
		}

		public void FinishPacketSlotPurchaseDialog(bool isYes)
		{
			KillDialog(51);
			if (isYes)
			{
				int itemCost = StoreScreen.GetItemCost(StoreItem.STORE_ITEM_PACKET_UPGRADE);
				mPlayerInfo.AddCoins(-itemCost);
				mPlayerInfo.mPurchases[21]++;
				WriteCurrentUserConfig();
				mBoard.mSeedBank.UpdateHeight();
				if (mCrazyDaveMessageIndex == 1503)
				{
					CrazyDaveTalkIndex(1510);
					return;
				}
				if (mCrazyDaveMessageIndex == 1553)
				{
					CrazyDaveTalkIndex(1560);
					return;
				}
			}
			else
			{
				mPlayerInfo.mDidntPurchasePacketUpgrade++;
				if (mCrazyDaveMessageIndex == 1503)
				{
					CrazyDaveTalkIndex(1520);
					return;
				}
				if (mCrazyDaveMessageIndex == 1553)
				{
					CrazyDaveTalkIndex(1570);
				}
			}
		}

		public void FinishTimesUpDialog()
		{
			KillDialog(42);
		}

		public void FinishPlantSale(bool isYes)
		{
			mZenGarden.DoPlantSale(isYes);
			KillDialog(48);
		}

		public void FinishLawnDialogMessageBox(bool isYes)
		{
			if (mLawnMessageBoxListener != null)
			{
				mLawnMessageBoxListener.LawnMessageBoxDone(isYes ? 1000 : 1001);
				mLawnMessageBoxListener = null;
				mWidgetManager.SetFocus(mOldFocus);
				mOldFocus = null;
			}
		}

		public void KillBoard()
		{
			FinishModelessDialogs();
			KillSeedChooserScreen();
			if (mBoard != null)
			{
				mBoard.DisposeBoard();
				mWidgetManager.RemoveWidget(mBoard);
				base.SafeDeleteWidget(mBoard);
				mBoard = null;
			}
		}

		public void MakeNewBoard()
		{
			KillBoard();
			mBoard = new Board(this);
			mBoard.Resize(Constants.Board_Offset_AspectRatio_Correction, 0, mWidth, mHeight);
			mWidgetManager.AddWidget(mBoard);
			mWidgetManager.BringToBack(mBoard);
			mWidgetManager.SetFocus(mBoard);
			GC.Collect();
			SexyAppBase.XnaGame.CompensateForSlowUpdate();
		}

		public void StartPlaying()
		{
			KillSeedChooserScreen();
			mBoard.StartLevel();
			mGameScene = GameScenes.SCENE_PLAYING;
		}

		public bool TryLoadGame()
		{
			string savedGameName = LawnCommon.GetSavedGameName(mGameMode, (int)mPlayerInfo.mId);
			mMusic.StopAllMusic();
			if (base.FileExists(savedGameName))
			{
				MakeNewBoard();
				if (mBoard.LoadGame(savedGameName))
				{
					mFirstTimeGameSelector = false;
					DoContinueDialog();
					return true;
				}
				KillBoard();
			}
			return false;
		}

		internal override void NewGame()
		{
			mFirstTimeGameSelector = false;
			MakeNewBoard();
			mBoard.InitLevel();
			mBoardResult = BoardResult.BOARDRESULT_NONE;
			mGameScene = GameScenes.SCENE_LEVEL_INTRO;
			ShowSeedChooserScreen();
			mBoard.mCutScene.StartLevelIntro();
		}

		public bool RestoreGame()
		{
			string savedGameName = LawnCommon.GetSavedGameName(mRestoreGameMode, (int)mPlayerInfo.mId);
			mMusic.StopAllMusic();
			if (base.FileExists(savedGameName))
			{
				mGameMode = mRestoreGameMode;
				MakeNewBoard();
				if (mBoard.LoadGame(savedGameName))
				{
					if (mBoard.mNextSurvivalStageCounter != 1)
					{
						string savedGameName2 = LawnCommon.GetSavedGameName(mRestoreGameMode, (int)mPlayerInfo.mId);
						base.EraseFile(savedGameName2);
					}
					mBoard.Pause(false);
					DoPauseDialog();
					return true;
				}
				KillBoard();
			}
			return false;
		}

		public void RestartLoopingSounds()
		{
			if (mGameMode == GameMode.GAMEMODE_CHALLENGE_RAINING_SEEDS || IsStormyNightLevel())
			{
				PlayFoley(FoleyType.FOLEY_RAIN);
			}
			int count = mBoard.mZombies.Count;
			for (int i = 0; i < count; i++)
			{
				Zombie zombie = mBoard.mZombies[i];
				if (!zombie.mDead && zombie.mPlayingSong)
				{
					zombie.mPlayingSong = false;
					zombie.StartZombieSound();
				}
			}
		}

		public void PreNewGame(GameMode theGameMode, bool theLookForSavedGame)
		{
			PreNewGame(theGameMode, theLookForSavedGame, true);
		}

		public void PreNewGame(GameMode theGameMode, bool theLookForSavedGame, bool checkForTutorialCompletion)
		{
			if (NeedRegister())
			{
				ShowGameSelector();
				return;
			}
			DelayLoadMainMenuResource(false);
			if (theGameMode == GameMode.GAMEMODE_CHALLENGE_ZEN_GARDEN)
			{
				DelayLoadZenGardenResources(true);
			}
			else
			{
				DelayLoadZenGardenResources(false);
			}
			GC.Collect();
			GC.WaitForPendingFinalizers();
			DelayLoadGamePlayResources(true);
			if (SexyAppBase.IsInTrialMode && mPlayerInfo.mLevel >= 7 && theGameMode != GameMode.GAMEMODE_CHALLENGE_ZEN_GARDEN)
			{
				if (mPlayerInfo.mNeedsTrialLevelReset)
				{
					mPlayerInfo.SetLevel(1);
					mPlayerInfo.mNeedsTrialLevelReset = false;
				}
				else
				{
					theGameMode = GameMode.GAMEMODE_UPSELL;
				}
			}
			if (SexyAppBase.IsInTrialMode && checkForTutorialCompletion && theGameMode == GameMode.GAMEMODE_ADVENTURE && mPlayerInfo.mLevel <= 3 && mPlayerInfo.mHasFinishedTutorial && mPlayerInfo.mFinishedAdventure == 0)
			{
				LawnDialog theDialog = DoDialog(58, true, string.Empty, "[SKIP_TUTORIAL_MESSAGE]", string.Empty, 1);
				LawnApp.CenterDialog(theDialog, (int)Constants.InvertAndScale(400f), (int)Constants.InvertAndScale(200f));
				return;
			}
			mGameMode = theGameMode;
			if (theLookForSavedGame && TryLoadGame())
			{
				return;
			}
			string savedGameName = LawnCommon.GetSavedGameName(mGameMode, (int)mPlayerInfo.mId);
			base.EraseFile(savedGameName);
			NewGame();
			if (mGameMode == GameMode.GAMEMODE_CHALLENGE_ZEN_GARDEN && !mPlayerInfo.mZenGardenTutorialComplete)
			{
				mZenGarden.SetupForZenTutorial();
			}
		}

		public void ShowGameSelectorWithOptions()
		{
			ShowGameSelector();
			DoNewOptions(true);
		}

		public void ShowGameSelector()
		{
			KillBoard();
			UpdateRegisterInfo();
			DelayLoadGamePlayResources(false);
			DelayLoadMainMenuResource(true);
			if (mGameSelector != null)
			{
				mWidgetManager.RemoveWidget(mGameSelector);
				base.SafeDeleteWidget(mGameSelector);
			}
			mGameScene = GameScenes.SCENE_MENU;
			mGameSelector = new GameSelector(this);
			mGameSelector.Resize(0, 0, Constants.GameSelector_Width, Constants.GameSelector_Height);
			mWidgetManager.AddWidget(mGameSelector);
			mWidgetManager.BringToBack(mGameSelector);
			mWidgetManager.SetFocus(mGameSelector);
			if (NeedRegister())
			{
				DoNeedRegisterDialog();
			}
		}

		public void KillGameSelector()
		{
			if (mGameSelector != null)
			{
				mWidgetManager.RemoveWidget(mGameSelector);
				base.SafeDeleteWidget(mGameSelector);
				mGameSelector = null;
			}
		}

		public void ShowGameSelectorQuickPlay(bool theDoFadeIn, GameSelectorButtons theButton)
		{
			ShowGameSelector();
			mGameSelector.mNeedToPlayRollIn = false;
			mGameSelector.MoveToQuickplay(theDoFadeIn, theButton);
		}

		public void ShowGameSelectorQuickPlay(bool theDoFadeIn)
		{
			ShowGameSelectorQuickPlay(theDoFadeIn, GameSelectorButtons.GameSelector_MiniGames);
		}

		protected override void ShowUpdateMessage()
		{
			SexyAppBase.UseLiveServers = false;
			if (Guide.IsVisible)
			{
				return;
			}
			Guide.BeginShowMessageBox(TodStringFile.TodStringTranslate("[UPDATE]"), TodStringFile.TodStringTranslate("[UPDATE_REQUIRED]"), new string[]
			{
				TodStringFile.TodStringTranslate("[BUTTON_YES]"),
				TodStringFile.TodStringTranslate("[BUTTON_NO]")
			}, 0, MessageBoxIcon.None, new AsyncCallback(GameUpdateMessageClosed), null);
			wantToShowUpdateMessage = false;
		}

		protected override bool ShowRunWhenLockedMessage()
		{
			if (Guide.IsVisible)
			{
				return false;
			}
			if (!TodStringFile.StringsLoaded)
			{
				return false;
			}
			/*Guide.BeginShowMessageBox(TodStringFile.TodStringTranslate("[ALLOW_RUN_WHEN_LOCKED_HEADING]"), TodStringFile.TodStringTranslate("[ALLOW_RUN_WHEN_LOCKED]"), new string[]
			{
				TodStringFile.TodStringTranslate("[BUTTON_YES]"),
				TodStringFile.TodStringTranslate("[BUTTON_NO]")
			}, 0, MessageBoxIcon.None, new AsyncCallback(this.RunWhenLockedMessageClosed), null);*/
			return true;
		}

		private void RunWhenLockedMessageClosed(IAsyncResult result)
		{
			Main.RunWhenLocked = (Guide.EndShowMessageBox(result) == 0);
			if (mPlayerInfo != null)
			{
				mPlayerInfo.mRunWhileLocked = Main.RunWhenLocked;
			}
		}

		private void GameUpdateMessageClosed(IAsyncResult result)
		{
			bool flag = Guide.EndShowMessageBox(result) == 0;
			if (!flag)
			{
				SexyAppBase.UseLiveServers = false;
				return;
			}
			if (Main.IsInTrialMode)
			{
				Guide.ShowMarketplace(PlayerIndex.One);
				return;
			}
			/*new MarketplaceDetailTask
			{
				ContentType = 1
			}.Show();*/
		}

		public void ShowAwardScreen(AwardType theAwardType, bool theShowAchievements)
		{
			mGameScene = GameScenes.SCENE_AWARD;
			mAwardScreen = new AwardScreen(this, theAwardType, theShowAchievements);
			mAwardScreen.Resize(0, 0, mWidth, mHeight);
			mWidgetManager.AddWidget(mAwardScreen);
			mWidgetManager.BringToBack(mAwardScreen);
			mWidgetManager.SetFocus(mAwardScreen);
		}

		public void KillAwardScreen()
		{
			if (mAwardScreen != null)
			{
				mWidgetManager.RemoveWidget(mAwardScreen);
				base.SafeDeleteWidget(mAwardScreen);
				mAwardScreen = null;
			}
		}

		public void ShowSeedChooserScreen()
		{
			Debug.ASSERT(mSeedChooserScreen == null);
			mSeedChooserScreen = new SeedChooserScreen();
			mSeedChooserScreen.Resize(0, 0, mWidth, mHeight);
			mWidgetManager.AddWidget(mSeedChooserScreen);
			mWidgetManager.BringToBack(mSeedChooserScreen);
		}

		public void KillSeedChooserScreen()
		{
			if (mSeedChooserScreen != null)
			{
				mWidgetManager.RemoveWidget(mSeedChooserScreen);
				base.SafeDeleteWidget(mSeedChooserScreen);
				mSeedChooserScreen = null;
			}
		}

		public void DoBackToMain()
		{
			DoBackToMain(true);
		}

		public void DoBackToMain(bool stopMusic)
		{
			if (stopMusic)
			{
				mMusic.StopAllMusic();
			}
			mSoundSystem.CancelPausedFoley();
			mSoundManager.StopAllSounds();
			WriteCurrentUserConfig();
			KillNewOptionsDialog();
			KillBoard();
			ShowGameSelector();
			mZenGarden.UnloadBackdrop();
		}

		public void DoConfirmBackToMain()
		{
			LawnDialog lawnDialog = DoDialog(22, true, "[LEAVE_GAME_HEADER]", "[LEAVE_GAME]", "", 1);
			lawnDialog.mLawnYesButton.mLabel = TodStringFile.TodStringTranslate("[LEAVE_BUTTON]");
			lawnDialog.mLawnNoButton.mLabel = TodStringFile.TodStringTranslate("[DIALOG_BUTTON_CANCEL]");
			lawnDialog.CalcSize(0, 0);
		}

		public void DoNewOptions(bool theFromGameSelector)
		{
			FinishModelessDialogs();
			NewOptionsDialog newOptionsDialog = new NewOptionsDialog(this, theFromGameSelector);
			int theWidth = (int)Constants.InvertAndScale(420f);
			int preferredHeight = newOptionsDialog.GetPreferredHeight(theWidth);
			LawnApp.CenterDialog(newOptionsDialog, theWidth, preferredHeight);
			base.AddDialog(2, newOptionsDialog);
			mWidgetManager.SetFocus(newOptionsDialog);
		}

		public void ShowLeaderboardDialog(LeaderBoardType aType)
		{
			FinishModelessDialogs();
			LeaderboardDialog leaderboardDialog = new LeaderboardDialog(this, aType);
			int theWidth = (int)Constants.InvertAndScale(420f);
			int preferredHeight = leaderboardDialog.GetPreferredHeight(theWidth);
			LawnApp.CenterDialog(leaderboardDialog, theWidth, preferredHeight);
			base.AddDialog(59, leaderboardDialog);
			mLeaderboardScreen.SetGrayed(true);
			mWidgetManager.SetFocus(leaderboardDialog);
		}

		public void DoRegister()
		{
		}

		public void DoRegisterError()
		{
			DoDialog(9, true, "[INVALID_CODE]", "[INVALID_CODE_MESSAGE]", "[DIALOG_BUTTON_OK]", 3);
		}

		public bool CanDoRegisterDialog()
		{
			return true;
		}

		public bool WriteCurrentUserConfig()
		{
			if (mPlayerInfo != null)
			{
				mPlayerInfo.SaveDetails();
			}
			return true;
		}

		public void WriteRestoreInfo()
		{
			RestoreLocation theValue = RestoreLocation.RESTORE_TITLESCREEN;
			if (mGameSelector != null)
			{
				theValue = RestoreLocation.RESTORE_MAINMENU;
			}
			else if (mBoard != null)
			{
				theValue = RestoreLocation.RESTORE_BOARD;
				base.RegistryWriteInteger("RestoreGameMode", (int)mGameMode);
			}
			base.RegistryWriteInteger("RestoreLocation", (int)theValue);
		}

		public void ReadRestoreInfo()
		{
			mRestoreLocation = RestoreLocation.RESTORE_MAINMENU;
			mRestoreLocation = (RestoreLocation)base.RegistryReadInteger("RestoreLocation");
			if (mRestoreLocation == RestoreLocation.RESTORE_BOARD)
			{
				mRestoreGameMode = (GameMode)base.RegistryReadInteger("RestoreGameMode");
			}
		}

		public void DoLockedAchievementDialog(AchievementId theId)
		{
			string fmt = TodStringFile.TodStringTranslate("[UNLOCK_TO_EARN]");
			string theDialogLines = Common.StrFormat_(fmt, Achievements.GetAchievementItem(theId).Name);
			LawnDialog lawnDialog = DoDialog(55, true, "[ACHIEVEMENT_UNLOCKED]", theDialogLines, "", 2);
			lawnDialog.mLawnYesButton.mLabel = TodStringFile.TodStringTranslate("[CONTINUE_BUTTON]");
			lawnDialog.mLawnNoButton.mLabel = TodStringFile.TodStringTranslate("[GET_FULL_VERSION_BUTTON]");
			lawnDialog.CalcSize(200, 10);
			LawnApp.CenterDialog(lawnDialog, lawnDialog.mWidth, lawnDialog.mHeight);
		}

		public void DoNeedRegisterDialog()
		{
			LawnDialog lawnDialog = DoDialog(5, true, "[REGISTER_HEADER]", "[REGISTER]", "", 2);
			lawnDialog.mLawnYesButton.mLabel = TodStringFile.TodStringTranslate("[REGISTER_BUTTON]");
			lawnDialog.mLawnNoButton.mLabel = TodStringFile.TodStringTranslate("[QUIT_BUTTON]");
		}

		public void DoContinueDialog()
		{
			ContinueDialog continueDialog = new ContinueDialog(this);
			LawnApp.CenterDialog(continueDialog, continueDialog.mWidth, continueDialog.mHeight);
			base.AddDialog(37, continueDialog);
		}

		public void DoPauseDialog()
		{
			mBoard.Pause(true);
			FinishModelessDialogs();
			LawnDialog lawnDialog = DoDialog(19, true, "[GAME_PAUSED]", "", "[RESUME_GAME]", 3);
			int num = Math.Max(Resources.FONT_DWARVENTODCRAFT15.StringWidth(TodStringFile.TodStringTranslate("[GAME_PAUSED]")), Resources.FONT_DWARVENTODCRAFT15.StringWidth(TodStringFile.TodStringTranslate("[RESUME_GAME]")));
			if ((float)num < Constants.InvertAndScale(125f))
			{
				num = (int)Constants.InvertAndScale(125f);
			}
			int num2 = AtlasResources.IMAGE_DIALOG_TOPLEFT.mWidth + num + AtlasResources.IMAGE_DIALOG_TOPRIGHT.mWidth;
			lawnDialog.mReanimation.AddReanimation((float)(num2 / 2) - Constants.InvertAndScale(85f), Constants.InvertAndScale(30f), ReanimationType.REANIM_ZOMBIE_NEWSPAPER);
			lawnDialog.mSpaceAfterHeader = (int)Constants.InvertAndScale(65f);
			lawnDialog.CalcSize((int)Constants.InvertAndScale(20f), (int)Constants.InvertAndScale(10f), num);
			LawnApp.CenterDialog(lawnDialog, lawnDialog.mWidth, lawnDialog.mHeight);
			if (mBoard.mCursorObject.mCursorType == CursorType.CURSOR_TYPE_HAMMER)
			{
				EnforceCursor();
			}
		}

		public void FinishModelessDialogs()
		{
		}

		public LawnDialog DoDialog(int theDialogId, bool isModal, string theDialogHeader, string theDialogLines, string theDialogFooter, int theButtonMode)
		{
			TodStringFile.TodStringTranslate(theDialogHeader);
			TodStringFile.TodStringTranslate(theDialogLines);
			TodStringFile.TodStringTranslate(theDialogFooter);
			LawnDialog lawnDialog = new LawnDialog(this, null, theDialogId, isModal, theDialogHeader, theDialogLines, theDialogFooter, theButtonMode);
			base.DoDialog(lawnDialog, theDialogId);
			if (mWidgetManager.mFocusWidget == null)
			{
				mWidgetManager.mFocusWidget = lawnDialog;
			}
			return lawnDialog;
		}

		public LawnDialog DoDialogDelay(int theDialogId, bool isModal, string theDialogHeader, string theDialogLines, string theDialogFooter, int theButtonMode)
		{
			LawnDialog lawnDialog = new LawnDialog(this, null, theDialogId, isModal, theDialogHeader, theDialogLines, theDialogFooter, theButtonMode);
			base.DoDialog(lawnDialog, theDialogId);
			lawnDialog.SetButtonDelay(30);
			return lawnDialog;
		}

		public override void Shutdown()
		{
			if (!mLoadingThreadCompleted)
			{
				mLoadingFailed = true;
				return;
			}
			if (!mShutdown)
			{
				for (int i = 0; i < 60; i++)
				{
					KillDialog(i);
				}
				if (mBoard != null)
				{
					mBoardResult = BoardResult.BOARDRESULT_QUIT_APP;
					mWidgetManager.mDownButtons = 0;
					mBoard.TryToSaveGame();
					KillBoard();
					WriteCurrentUserConfig();
				}
				mProfileMgr.Save();
				base.ProcessSafeDeleteList();
				if (mZenGarden != null)
				{
					mZenGarden = null;
				}
				if (mEffectSystem != null)
				{
					mEffectSystem.EffectSystemDispose();
					mEffectSystem.Dispose();
					mEffectSystem = null;
				}
				if (mReanimatorCache != null)
				{
					mReanimatorCache.ReanimatorCacheDispose();
					mReanimatorCache = null;
				}
				TodParticleGlobal.TodParticleFreeDefinitions();
				ReanimatorXnaHelpers.ReanimatorFreeDefinitions();
				Coin.CoinFreeTextures();
				GlobalMembersTrail.TrailFreeDefinitions();
				UpdateRegisterInfo();
				base.Shutdown();
			}
		}

		public override void Init()
		{
			bool flag = mTodCheatKeys;
			mSessionID = (int)DateTime.UtcNow.Ticks;
			mPlayTimeActiveSession = 0;
			mPlayTimeInactiveSession = 0;
			mBoardResult = BoardResult.BOARDRESULT_NONE;
			mKilledYetiAndRestarted = false;
			base.Init();
			ReadRestoreInfo();
			if (!mResourceManager.ParseResourcesFile("Content/resources.xml"))
			{
				ShowResourceError(true);
				return;
			}
			if (Constants.Language != Constants.LanguageIndex.de)
			{
				if (!TodCommon.TodLoadResources("Init"))
				{
					return;
				}
			}
			else if (!TodCommon.TodLoadResources("InitRegistered"))
			{
				return;
			}
			Resources.ExtractInitResources(mResourceManager);
			PerfTimer perfTimer = default(PerfTimer);
			perfTimer.Start();
			mProfileMgr.Load();
			string empty = string.Empty;
			if (mPlayerInfo == null && RegistryReadString("CurUser", empty) != null)
			{
				mPlayerInfo = mProfileMgr.GetProfile(empty);
			}
			try
			{
				if (MediaPlayer.GameHasControl)
				{
					MediaPlayer.Play(mContentManager.Load<Song>(GlobalStaticVars.GetResourceDir() + "music/crazydave"));
				}
			}
			catch (Exception)
			{
			}
			if (mPlayerInfo == null)
			{
				PlayerInfo anyProfile = mProfileMgr.GetAnyProfile();
				if (!MediaPlayer.GameHasControl)
				{
					anyProfile.mMusicVolume = (double)MediaPlayer.Volume;
				}
				mPlayerInfo = anyProfile;
			}
			mMaxExecutions = base.GetInteger("MaxExecutions", 0);
			mMaxPlays = base.GetInteger("MaxPlays", 0);
			mMaxTime = base.GetInteger("MaxTime", 60);
			mTitleScreen = new TitleScreen(this);
			mTitleScreen.Resize(0, 0, mWidth, mHeight);
			mWidgetManager.AddWidget(mTitleScreen);
			mWidgetManager.SetFocus(mTitleScreen);
			perfTimer.Start();
			mMusic = new Music();
			mSoundSystem = new TodFoley();
			mEffectSystem = new EffectSystem();
			mEffectSystem.EffectSystemInitialize();
			FilterEffect.FilterEffectInitForApp();
			mKonamiCheck = new TypingCheck();
			mKonamiCheck.AddKeyCode(KeyCode.KEYCODE_UP);
			mKonamiCheck.AddKeyCode(KeyCode.KEYCODE_UP);
			mKonamiCheck.AddKeyCode(KeyCode.KEYCODE_DOWN);
			mKonamiCheck.AddKeyCode(KeyCode.KEYCODE_DOWN);
			mKonamiCheck.AddKeyCode(KeyCode.KEYCODE_LEFT);
			mKonamiCheck.AddKeyCode(KeyCode.KEYCODE_RIGHT);
			mKonamiCheck.AddKeyCode(KeyCode.KEYCODE_LEFT);
			mKonamiCheck.AddKeyCode(KeyCode.KEYCODE_RIGHT);
			mKonamiCheck.AddChar('b');
			mKonamiCheck.AddChar('a');
			mMustacheCheck = new TypingCheck("mustache");
			mMoustacheCheck = new TypingCheck("moustache");
			mSuperMowerCheck = new TypingCheck("trickedout");
			mSuperMowerCheck2 = new TypingCheck("tricked out");
			mFutureCheck = new TypingCheck("future");
			mPinataCheck = new TypingCheck("pinata");
			mDanceCheck = new TypingCheck("dance");
			mDaisyCheck = new TypingCheck("daisies");
			mSukhbirCheck = new TypingCheck("sukhbir");
			perfTimer.Start();
			perfTimer.Start();
		}

		public override void Start()
		{
			if (mLoadingFailed)
			{
				return;
			}
			base.Start();
		}

		public string RegistryReadString(string key, string value)
		{
			return null;
		}

		public virtual Dialog NewDialog(int theDialogId, bool isModal, string theDialogHeader, string theDialogLines, string theDialogFooter, int theButtonMode)
		{
			LawnDialog lawnDialog = new LawnDialog(this, null, theDialogId, isModal, theDialogHeader, theDialogLines, theDialogFooter, theButtonMode);
			if (lawnDialog.mWidth < 380)
			{
				lawnDialog.mWidth = 380;
			}
			if (lawnDialog.mHeight > 320)
			{
				lawnDialog.mHeight = 320;
			}
			LawnApp.CenterDialog(lawnDialog, lawnDialog.mWidth, lawnDialog.mHeight);
			return lawnDialog;
		}

		public override bool KillDialog(int theDialogId)
		{
			if (base.KillDialog(theDialogId))
			{
				if (mDialogMap.Count == 0)
				{
					if (mBoard != null)
					{
						mWidgetManager.SetFocus(mBoard);
					}
					else if (mGameSelector != null)
					{
						mWidgetManager.SetFocus(mGameSelector);
					}
				}
				if (mBoard != null && !NeedPauseGame())
				{
					mBoard.Pause(false);
				}
				return true;
			}
			return false;
		}

		public override void ModalOpen()
		{
			if (mBoard != null && NeedPauseGame())
			{
				mBoard.Pause(true);
			}
		}

		public override void ModalClose()
		{
		}

		public void PreDisplayHook()
		{
			PreDisplayHook();
		}

		public bool ChangeDirHook(string theIntendedPath)
		{
			return false;
		}

		public bool NeedRegister()
		{
			return false;
		}

		public void UpdateRegisterInfo()
		{
		}

		public override void ButtonPress(int theId)
		{
		}

		public override void ButtonDepress(int theId)
		{
			if (theId % 10000 >= 2000 && theId % 10000 < 3000)
			{
				int num = theId - 2000;
				switch (num)
				{
				case 0:
					KillDialog(0);
					ShowGameSelector();
					return;
				case 1:
				case 3:
				case 4:
				case 8:
				case 9:
				case 10:
				case 11:
				case 12:
				case 14:
				case 17:
				case 18:
				case 24:
				case 25:
				case 26:
				case 27:
				case 36:
				case 37:
				case 38:
				case 40:
				case 41:
				case 43:
				case 44:
				case 45:
				case 50:
				case 54:
				case 56:
				case 57:
					break;
				case 2:
					KillNewOptionsDialog();
					if (mBoard != null)
					{
						RestartLoopingSounds();
						return;
					}
					return;
				case 5:
					DoRegister();
					return;
				case 6:
					return;
				case 7:
					KillDialog(7);
					CheckForUpdates();
					return;
				case 13:
					KillDialog(13);
					CloseRequestAsync();
					return;
				case 15:
					KillDialog(15);
					DoRegister();
					return;
				case 16:
					KillDialog(16);
					return;
				case 19:
					KillDialog(19);
					return;
				case 20:
					KillDialog(20);
					mBoard.AddSunMoney(100);
					return;
				case 21:
					KillDialog(21);
					return;
				case 22:
					KillDialog(22);
					mBoardResult = BoardResult.BOARDRESULT_QUIT;
					mBoard.TryToSaveGame();
					DoBackToMain();
					return;
				case 23:
					FinishInGameRestartConfirmDialog(true);
					return;
				case 28:
				case 49:
					KillDialog(theId - 2000);
					FinishLawnDialogMessageBox(true);
					return;
				case 29:
					FinishUserDialog(true);
					return;
				case 30:
					FinishCreateUserDialog(true);
					return;
				case 31:
					FinishConfirmDeleteUserDialog(true);
					return;
				case 32:
					FinishRenameUserDialog(true);
					return;
				case 33:
					FinishNameError(theId - 2000);
					return;
				case 34:
					FinishNameError(theId - 2000);
					return;
				case 35:
					FinishCheatDialog(true);
					return;
				case 39:
					FinishRestartConfirmDialog();
					return;
				case 42:
					FinishTimesUpDialog();
					return;
				case 46:
					((StoreScreen)base.GetDialog(4)).PurchasePendingItem();
					return;
				case 47:
					((StoreScreen)base.GetDialog(4)).FinishTreeOfWisdomDialog(true);
					return;
				case 48:
					FinishPlantSale(true);
					return;
				case 51:
					FinishPacketSlotPurchaseDialog(true);
					return;
				case 52:
					FinishAboutDialog(true);
					return;
				case 53:
					FinishRestartWarningDialog(true);
					return;
				case 55:
					KillDialog(55);
					return;
				case 58:
					KillDialog(theId - 2000);
					PreNewGame(GameMode.GAMEMODE_ADVENTURE, true, false);
					mPlayerInfo.mHasFinishedTutorial = false;
					return;
				default:
					if (num == 20008)
					{
						KillDialog(20008);
						KillDialog(8);
						return;
					}
					break;
				}
				KillDialog(theId - 2000);
				return;
			}
			if (theId % 10000 >= 3000 && theId % 10000 < 4000)
			{
				int num2 = theId - 3000;
				if (num2 <= 42)
				{
					switch (num2)
					{
					case 5:
						KillDialog(5);
						Shutdown();
						return;
					case 6:
						KillDialog(6);
						return;
					default:
						switch (num2)
						{
						case 23:
							FinishInGameRestartConfirmDialog(false);
							return;
						case 24:
						case 25:
						case 26:
						case 27:
						case 33:
						case 34:
							goto IL_47F;
						case 28:
							break;
						case 29:
							FinishUserDialog(false);
							return;
						case 30:
							FinishCreateUserDialog(false);
							return;
						case 31:
							FinishConfirmDeleteUserDialog(false);
							return;
						case 32:
							FinishRenameUserDialog(false);
							return;
						case 35:
							FinishCheatDialog(false);
							return;
						default:
							if (num2 != 42)
							{
								goto IL_47F;
							}
							FinishTimesUpDialog();
							return;
						}
						break;
					}
				}
				else
				{
					switch (num2)
					{
					case 47:
						((StoreScreen)base.GetDialog(4)).FinishTreeOfWisdomDialog(false);
						return;
					case 48:
						FinishPlantSale(false);
						return;
					case 49:
						break;
					case 50:
					case 52:
					case 53:
					case 54:
						goto IL_47F;
					case 51:
						FinishPacketSlotPurchaseDialog(false);
						return;
					case 55:
						checkGiveAchievements = true;
						BuyGame();
						return;
					default:
						if (num2 == 58)
						{
							KillDialog(theId - 3000);
							mPlayerInfo.SetLevel(4);
							PreNewGame(GameMode.GAMEMODE_ADVENTURE, false, false);
							return;
						}
						if (num2 != 10008)
						{
							goto IL_47F;
						}
						KillDialog(10008);
						KillDialog(8);
						return;
					}
				}
				KillDialog(theId - 3000);
				FinishLawnDialogMessageBox(false);
				return;
				IL_47F:
				KillDialog(theId - 3000);
			}
		}

		public override void LeftTrialMode()
		{
			base.LeftTrialMode();
			if (mGameSelector != null)
			{
				mGameSelector.SyncButtons();
			}
		}

		public override void UpdateFrames()
		{
			if (wantToShowUpdateMessage)
			{
				ShowUpdateMessage();
			}
			if (LoadingScreen.IsLoading)
			{
				LoadingScreen.gLoadingScreen.Update();
				return;
			}
			UpdatePlayTimeStats();
			int num = 1;
			if (GlobalStaticVars.gSlowMo)
			{
				GlobalStaticVars.gSlowMoCounter++;
				if (GlobalStaticVars.gSlowMoCounter >= 4)
				{
					GlobalStaticVars.gSlowMoCounter = 0;
				}
				else
				{
					num = 0;
				}
			}
			else if (GlobalStaticVars.gFastMo)
			{
				num = 20;
			}
			for (int i = 0; i < num; i++)
			{
				mAppCounter++;
				if (mBoard != null)
				{
					mBoard.ProcessDeleteQueue();
				}
				base.UpdateFrames();
				if (mLoadingThreadCompleted && mEffectSystem != null)
				{
					mEffectSystem.ProcessDeleteQueue();
				}
				CheckForGameEnd();
			}
		}

		public bool IsAdventureMode()
		{
			return mGameMode == GameMode.GAMEMODE_ADVENTURE;
		}

		public bool IsQuickPlayMode()
		{
			return mGameMode >= GameMode.GAMEMODE_QUICKPLAY_1 && mGameMode <= GameMode.GAMEMODE_QUICKPLAY_50;
		}

		public bool IsSurvivalMode()
		{
			return mGameMode >= GameMode.GAMEMODE_SURVIVAL_NORMAL_STAGE_1 && mGameMode <= GameMode.GAMEMODE_SURVIVAL_ENDLESS_STAGE_5;
		}

		public bool IsContinuousChallenge()
		{
			return IsArtChallenge() || IsSlotMachineLevel() || IsFinalBossLevel() || mGameMode == GameMode.GAMEMODE_CHALLENGE_BEGHOULED || mGameMode == GameMode.GAMEMODE_UPSELL || mGameMode == GameMode.GAMEMODE_INTRO || mGameMode == GameMode.GAMEMODE_CHALLENGE_BEGHOULED_TWIST;
		}

		public bool IsArtChallenge()
		{
			return mBoard != null && (mGameMode == GameMode.GAMEMODE_CHALLENGE_ART_CHALLENGE_1 || mGameMode == GameMode.GAMEMODE_CHALLENGE_ART_CHALLENGE_2 || mGameMode == GameMode.GAMEMODE_CHALLENGE_SEEING_STARS);
		}

		public bool NeedPauseGame()
		{
			if (mDialogList.Count == 0)
			{
				return false;
			}
			int num = 0;
			if (mDialogList.Count == 1 && mDialogList.First.Value.mId != 0)
			{
				num = mDialogList.First.Value.mId;
			}
			return num != 28 && num != 51 && num != 50 && (mBoard == null || mGameMode != GameMode.GAMEMODE_CHALLENGE_ZEN_GARDEN) && (mBoard == null || mGameMode != GameMode.GAMEMODE_TREE_OF_WISDOM);
		}

		public void ShowResourceError()
		{
			ShowResourceError(false);
		}

		public override void ShowResourceError(bool doExit)
		{
			base.ShowResourceError(doExit);
		}

		public void ToggleSlowMo()
		{
			GlobalStaticVars.gSlowMoCounter = 0;
			GlobalStaticVars.gSlowMo = !GlobalStaticVars.gSlowMo;
			GlobalStaticVars.gFastMo = false;
		}

		public void ToggleFastMo()
		{
			GlobalStaticVars.gFastMo = !GlobalStaticVars.gFastMo;
			GlobalStaticVars.gSlowMo = false;
		}

		public void PlayFoley(FoleyType theFoleyType)
		{
			if (!mMuteSoundsForCutscene)
			{
				mSoundSystem.PlayFoley(theFoleyType);
			}
		}

		public void PlayFoleyPitch(FoleyType theFoleyType, float aPitch)
		{
			if (!mMuteSoundsForCutscene)
			{
				mSoundSystem.PlayFoleyPitch(theFoleyType, aPitch);
			}
		}

		public void FastLoad(GameMode theGameMode)
		{
			if (mShutdown)
			{
				return;
			}
			mWidgetManager.RemoveWidget(mTitleScreen);
			base.SafeDeleteWidget(mTitleScreen);
			mTitleScreen = null;
			PreNewGame(theGameMode, false);
		}

		public string GetStageString(int theLevel)
		{
			string text;
			if (!cachedStageStrings.TryGetValue(theLevel, out text))
			{
				int num = TodCommon.ClampInt((theLevel - 1) / 10 + 1, 1, 6);
				int num2 = theLevel - (num - 1) * 10;
				text = Common.StrFormat_(TodStringFile.TodStringTranslate("[STAGE_STRING]"), num, num2);
				cachedStageStrings.Add(theLevel, text);
			}
			return text;
		}

		public void KillChallengeScreen()
		{
			if (mChallengeScreen != null)
			{
				mWidgetManager.RemoveWidget(mChallengeScreen);
				base.SafeDeleteWidget(mChallengeScreen);
				mChallengeScreen = null;
			}
		}

		public void ShowChallengeScreen(ChallengePage thePage)
		{
			mGameScene = GameScenes.SCENE_CHALLENGE;
			mChallengeScreen = new ChallengeScreen(this, thePage);
			mChallengeScreen.Resize(0, 0, mWidth, mHeight);
			mWidgetManager.AddWidget(mChallengeScreen);
			mWidgetManager.BringToBack(mChallengeScreen);
			mWidgetManager.SetFocus(mChallengeScreen);
		}

		public void ShowLeaderboardScreen()
		{
			mGameScene = GameScenes.SCENE_LEADERBOARD;
			mLeaderboardScreen = new LeaderboardScreen(this);
			mLeaderboardScreen.Resize(0, 0, mWidth, mHeight);
			mWidgetManager.AddWidget(mLeaderboardScreen);
			mWidgetManager.BringToBack(mLeaderboardScreen);
			mWidgetManager.SetFocus(mLeaderboardScreen);
		}

		public void KillLeaderboardScreen()
		{
			if (mLeaderboardScreen != null)
			{
				mLeaderboardScreen.UnloadResources();
				mWidgetManager.RemoveWidget(mLeaderboardScreen);
				base.SafeDeleteWidget(mLeaderboardScreen);
				mLeaderboardScreen = null;
			}
		}

		public void CheckForGameEnd()
		{
			if (mBoard == null || !mBoard.mLevelComplete)
			{
				return;
			}
			bool flag = mBoard.CheckForPostGameAchievements();
			flag = false;
			UpdatePlayerProfileForFinishingLevel();
			if (IsAdventureMode())
			{
				int level = mBoard.mLevel;
				KillBoard();
				if (IsFirstTimeAdventureMode() && level < 50)
				{
					ShowAwardScreen(AwardType.AWARD_FOR_LEVEL, flag);
					return;
				}
				if (level == 50)
				{
					if (mPlayerInfo.mFinishedAdventure != 1)
					{
						ShowAwardScreen(AwardType.AWARD_FOR_LEVEL, flag);
						return;
					}
					ShowAwardScreen(AwardType.AWARD_PRE_CREDITS_ZOMBIE_NOTE, flag);
					return;
				}
				else
				{
					if (level == 9 || level == 19 || level == 29 || level == 39 || level == 49)
					{
						ShowAwardScreen(AwardType.AWARD_FOR_LEVEL, flag);
						return;
					}
					if (flag)
					{
						ShowAwardScreen(AwardType.AWARD_ACHIEVEMENT_ONLY, true);
						return;
					}
					PreNewGame(mGameMode, false);
					return;
				}
			}
			else if (IsQuickPlayMode())
			{
				KillBoard();
				if (flag)
				{
					ShowAwardScreen(AwardType.AWARD_ACHIEVEMENT_ONLY, flag);
					return;
				}
				ShowGameSelectorQuickPlay(false);
				return;
			}
			else if (IsSurvivalMode())
			{
				if (mBoard.IsFinalSurvivalStage())
				{
					KillBoard();
					ShowGameSelectorQuickPlay(false);
					return;
				}
				mBoard.mChallenge.mSurvivalStage++;
				KillSeedChooserScreen();
				mBoard.InitSurvivalStage();
				return;
			}
			else
			{
				if (!IsPuzzleMode())
				{
					KillBoard();
					ShowGameSelectorQuickPlay(false);
					return;
				}
				bool flag2 = IsIZombieLevel();
				KillBoard();
				if (flag2)
				{
					ShowGameSelectorQuickPlay(false, GameSelectorButtons.GameSelector_IZombie);
					return;
				}
				ShowGameSelectorQuickPlay(false, GameSelectorButtons.GameSelector_Vasebreaker);
				return;
			}
		}

		public virtual void CloseRequestAsync()
		{
			mCloseRequest = true;
		}

		public bool IsChallengeWithoutSeedBank()
		{
			return mGameMode == GameMode.GAMEMODE_CHALLENGE_RAINING_SEEDS || mGameMode == GameMode.GAMEMODE_UPSELL || mGameMode == GameMode.GAMEMODE_INTRO || IsWhackAZombieLevel() || IsSquirrelLevel() || IsScaryPotterLevel() || mGameMode == GameMode.GAMEMODE_CHALLENGE_ZEN_GARDEN || mGameMode == GameMode.GAMEMODE_TREE_OF_WISDOM;
		}

		public AlmanacDialog DoAlmanacDialog(SeedType theSeedType, ZombieType theZombieType, AlmanacListener theListener)
		{
			default(PerfTimer).Start();
			FinishModelessDialogs();
			AlmanacDialog almanacDialog = new AlmanacDialog(this, theListener);
			almanacDialog.Resize(0, 0, Constants.BackBufferSize.Y, Constants.BackBufferSize.X);
			base.AddDialog(3, almanacDialog);
			mWidgetManager.SetFocus(almanacDialog);
			if (theSeedType != SeedType.SEED_NONE)
			{
				almanacDialog.ShowPlant(theSeedType);
			}
			else if (theZombieType != ZombieType.ZOMBIE_INVALID)
			{
				almanacDialog.ShowZombie(theZombieType);
			}
			return almanacDialog;
		}

		public bool KillAlmanacDialog()
		{
			if ((AlmanacDialog)base.GetDialog(Dialogs.DIALOG_ALMANAC) == null)
			{
				return false;
			}
			KillDialog(3);
			return true;
		}

		public int GetSeedsAvailable()
		{
			int level = mPlayerInfo.GetLevel();
			if (HasFinishedAdventure() || level > 50)
			{
				return 49;
			}
			int awardSeedForLevel = (int)GetAwardSeedForLevel(level);
			return Math.Min(49, awardSeedForLevel);
		}

		public Reanimation AddReanimation(float theX, float theY, int aRenderOrder, ReanimationType theReanimationType)
		{
			return AddReanimation(theX, theY, aRenderOrder, theReanimationType, true);
		}

		public Reanimation AddReanimation(float theX, float theY, int aRenderOrder, ReanimationType theReanimationType, bool theDoScalePos)
		{
			if (theDoScalePos)
			{
				theX *= Constants.S;
				theY *= Constants.S;
			}
			return mEffectSystem.mReanimationHolder.AllocReanimation(theX, theY, aRenderOrder, theReanimationType);
		}

		public TodParticleSystem AddTodParticle(float theX, float theY, int aRenderOrder, ParticleEffect theEffect)
		{
			return mEffectSystem.mParticleHolder.AllocParticleSystem(theX, theY, aRenderOrder, theEffect);
		}

		public TodParticleSystem ParticleGetID(TodParticleSystem theParticle)
		{
			return theParticle;
		}

		public TodParticleSystem ParticleGet(TodParticleSystem theParticleID)
		{
			return theParticleID;
		}

		public TodParticleSystem ParticleTryToGet(TodParticleSystem theParticleID)
		{
			if (theParticleID == null || !theParticleID.mActive)
			{
				return null;
			}
			return theParticleID;
		}

		public Reanimation ReanimationGetID(Reanimation theReanimation)
		{
			if (theReanimation == null || theReanimation.mDead)
			{
				return null;
			}
			return theReanimation;
		}

		public Reanimation ReanimationGet(Reanimation theReanimID)
		{
			if (theReanimID == null || !theReanimID.mActive)
			{
				return null;
			}
			return theReanimID;
		}

		public Reanimation ReanimationTryToGet(Reanimation theReanimID)
		{
			if (theReanimID == null || !theReanimID.mActive)
			{
				return null;
			}
			return theReanimID;
		}

		public void RemoveReanimation(ref Reanimation theReanimationID)
		{
			Reanimation reanimation = ReanimationTryToGet(theReanimationID);
			if (reanimation != null)
			{
				reanimation.ReanimationDie();
			}
			theReanimationID = null;
		}

		public void RemoveParticle(TodParticleSystem theParticleID)
		{
			TodParticleSystem todParticleSystem = ParticleTryToGet(theParticleID);
			if (todParticleSystem != null)
			{
				todParticleSystem.ParticleSystemDie();
			}
		}

		public StoreScreen ShowStoreScreen(StoreListener theListener)
		{
			DelayLoadGamePlayResources(false);
			DelayLoadCachedResources(true);
			DelayLoadMainMenuResource(true);
			DelayLoadZenGardenResources(true);
			FinishModelessDialogs();
			Debug.ASSERT(base.GetDialog(Dialogs.DIALOG_STORE) == null);
			StoreScreen storeScreen = new StoreScreen(this, theListener);
			base.AddDialog(4, storeScreen);
			mWidgetManager.SetFocus(storeScreen);
			return storeScreen;
		}

		public UpsellScreen ShowUpsellScreen()
		{
			FinishModelessDialogs();
			UpsellScreen upsellScreen = new UpsellScreen(this);
			base.AddDialog(54, upsellScreen);
			mWidgetManager.SetFocus(upsellScreen);
			return upsellScreen;
		}

		public void KillStoreScreen()
		{
			if ((AlmanacDialog)base.GetDialog(Dialogs.DIALOG_STORE) == null)
			{
				return;
			}
			KillDialog(4);
		}

		public bool HasSeedType(SeedType theSeedType)
		{
			if (theSeedType == SeedType.SEED_GATLINGPEA)
			{
				return mPlayerInfo.mPurchases[0] > 0;
			}
			if (IsTrialStageLocked() && theSeedType >= SeedType.SEED_JALAPENO)
			{
				return false;
			}
			if (theSeedType == SeedType.SEED_TWINSUNFLOWER)
			{
				return mPlayerInfo.mPurchases[1] > 0;
			}
			if (theSeedType == SeedType.SEED_GLOOMSHROOM)
			{
				return mPlayerInfo.mPurchases[2] > 0;
			}
			if (theSeedType == SeedType.SEED_CATTAIL)
			{
				return mPlayerInfo.mPurchases[3] > 0;
			}
			if (theSeedType == SeedType.SEED_WINTERMELON)
			{
				return mPlayerInfo.mPurchases[4] > 0;
			}
			if (theSeedType == SeedType.SEED_GOLD_MAGNET)
			{
				return mPlayerInfo.mPurchases[5] > 0;
			}
			if (theSeedType == SeedType.SEED_SPIKEROCK)
			{
				return mPlayerInfo.mPurchases[6] > 0;
			}
			if (theSeedType == SeedType.SEED_COBCANNON)
			{
				return mPlayerInfo.mPurchases[7] > 0;
			}
			if (theSeedType == SeedType.SEED_IMITATER)
			{
				return mPlayerInfo.mPurchases[8] > 0;
			}
			return theSeedType < (SeedType)GetSeedsAvailable();
		}

		public void EndLevel()
		{
			KillBoard();
			if (IsAdventureMode())
			{
				NewGame();
			}
		}

		public bool IsIceDemo()
		{
			return false;
		}

		public bool IsShovelLevel()
		{
			return mBoard != null && mGameMode == GameMode.GAMEMODE_CHALLENGE_SHOVEL;
		}

		public bool IsWallnutBowlingLevel()
		{
			return mBoard != null && (mGameMode == GameMode.GAMEMODE_CHALLENGE_WALLNUT_BOWLING || mGameMode == GameMode.GAMEMODE_CHALLENGE_WALLNUT_BOWLING_2 || ((IsAdventureMode() && mPlayerInfo.mLevel == 5) || mGameMode == GameMode.GAMEMODE_QUICKPLAY_5));
		}

		public bool IsMiniBossLevel()
		{
			return mBoard != null && ((IsAdventureMode() && mPlayerInfo.mLevel == 10) || mGameMode == GameMode.GAMEMODE_QUICKPLAY_10 || ((IsAdventureMode() && mPlayerInfo.mLevel == 20) || mGameMode == GameMode.GAMEMODE_QUICKPLAY_20) || ((IsAdventureMode() && mPlayerInfo.mLevel == 30) || mGameMode == GameMode.GAMEMODE_QUICKPLAY_30));
		}

		public bool IsSlotMachineLevel()
		{
			return mBoard != null && mGameMode == GameMode.GAMEMODE_CHALLENGE_SLOT_MACHINE;
		}

		public bool IsLittleTroubleLevel()
		{
			return mBoard != null && (mGameMode == GameMode.GAMEMODE_CHALLENGE_LITTLE_TROUBLE || ((IsAdventureMode() && mPlayerInfo.mLevel == 25) || mGameMode == GameMode.GAMEMODE_QUICKPLAY_25));
		}

		public bool IsStormyNightLevel()
		{
			return mBoard != null && (mGameMode == GameMode.GAMEMODE_CHALLENGE_STORMY_NIGHT || ((IsAdventureMode() && mPlayerInfo.mLevel == 40) || mGameMode == GameMode.GAMEMODE_QUICKPLAY_40));
		}

		public bool IsFinalBossLevel()
		{
			return mBoard != null && (mGameMode == GameMode.GAMEMODE_CHALLENGE_FINAL_BOSS || ((IsAdventureMode() && mPlayerInfo.mLevel == 50) || mGameMode == GameMode.GAMEMODE_QUICKPLAY_50));
		}

		public bool IsBungeeBlitzLevel()
		{
			return mBoard != null && (mGameMode == GameMode.GAMEMODE_CHALLENGE_BUNGEE_BLITZ || ((IsAdventureMode() && mPlayerInfo.mLevel == 45) || mGameMode == GameMode.GAMEMODE_QUICKPLAY_45));
		}

		public SeedType GetAwardSeedForLevel(int theLevel)
		{
			int num = (theLevel - 1) / 10 + 1;
			int num2 = (theLevel - 1) % 10 + 1;
			int num3 = (num - 1) * 8 + num2;
			if (num2 >= 10)
			{
				num3 -= 2;
			}
			else if (num2 >= 5)
			{
				num3--;
			}
			if (num3 > 40)
			{
				num3 = 40;
			}
			return (SeedType)num3;
		}

		public string GetCrazyDaveText(int theMessageIndex)
		{
			string theText = Common.StrFormat_("[CRAZY_DAVE_{0}]", theMessageIndex);
			theText = TodCommon.TodReplaceString(theText, "{PLAYER_NAME}", mPlayerInfo.mName);
			theText = TodCommon.TodReplaceString(theText, "{MONEY}", LawnApp.GetMoneyString(mPlayerInfo.mCoins));
			int itemCost = StoreScreen.GetItemCost(StoreItem.STORE_ITEM_PACKET_UPGRADE);
			return TodCommon.TodReplaceString(theText, "{UPGRADE_COST}", LawnApp.GetMoneyString(itemCost));
		}

		public bool CanShowAlmanac()
		{
			return !IsIceDemo() && mPlayerInfo != null && (HasFinishedAdventure() || mPlayerInfo.mLevel >= 15);
		}

		public bool IsNight()
		{
			return (mBoard != null && mBoard.StageIsNight()) || (!IsIceDemo() && mPlayerInfo != null && ((mPlayerInfo.mLevel >= 11 && mPlayerInfo.mLevel <= 20) || (mPlayerInfo.mLevel >= 31 && mPlayerInfo.mLevel <= 40) || mPlayerInfo.mLevel == 50));
		}

		public bool CanShowStore()
		{
			return !IsIceDemo() && mPlayerInfo != null && (HasFinishedAdventure() || mPlayerInfo.mHasSeenUpsell || mPlayerInfo.mLevel >= 25);
		}

		public bool HasBeatenChallenge(GameMode theGameMode)
		{
			if (mPlayerInfo == null)
			{
				return false;
			}
			int num = theGameMode - GameMode.GAMEMODE_SURVIVAL_NORMAL_STAGE_1;
			Debug.ASSERT(num >= 0 && num < 122);
			if (IsSurvivalNormal(theGameMode))
			{
				return mPlayerInfo.mChallengeRecords[num] >= 5;
			}
			if (IsSurvivalHard(theGameMode))
			{
				return mPlayerInfo.mChallengeRecords[num] >= 10;
			}
			return !IsSurvivalEndless(theGameMode) && !IsEndlessScaryPotter(theGameMode) && !IsEndlessIZombie(theGameMode) && mPlayerInfo.mChallengeRecords[num] > 0;
		}

		public PottedPlant GetPottedPlantByIndex(int thePottedPlantIndex)
		{
			Debug.ASSERT(thePottedPlantIndex >= 0 && thePottedPlantIndex < mPlayerInfo.mNumPottedPlants);
			return mPlayerInfo.mPottedPlant[thePottedPlantIndex];
		}

		public bool IsSurvivalNormal(GameMode theGameMode)
		{
			return theGameMode >= GameMode.GAMEMODE_SURVIVAL_NORMAL_STAGE_1 && theGameMode <= GameMode.GAMEMODE_SURVIVAL_NORMAL_STAGE_5;
		}

		public bool IsSurvivalHard(GameMode theGameMode)
		{
			return theGameMode >= GameMode.GAMEMODE_SURVIVAL_HARD_STAGE_1 && theGameMode <= GameMode.GAMEMODE_SURVIVAL_HARD_STAGE_5;
		}

		public bool IsSurvivalEndless(GameMode theGameMode)
		{
			return theGameMode >= GameMode.GAMEMODE_SURVIVAL_ENDLESS_STAGE_1 && theGameMode <= GameMode.GAMEMODE_SURVIVAL_ENDLESS_STAGE_5;
		}

		public bool HasFinishedAdventure()
		{
			return mPlayerInfo != null && mPlayerInfo.mFinishedAdventure > 0;
		}

		public bool IsFirstTimeAdventureMode()
		{
			return IsAdventureMode() && !HasFinishedAdventure();
		}

		public bool CanSpawnYetis()
		{
			ZombieDefinition zombieDefinition = Zombie.GetZombieDefinition(ZombieType.ZOMBIE_YETI);
			return HasFinishedAdventure() && (mPlayerInfo.mFinishedAdventure >= 2 || mPlayerInfo.mLevel >= zombieDefinition.mStartingLevel);
		}

		public void CrazyDaveEnter()
		{
			Debug.ASSERT(mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_OFF);
			Debug.ASSERT(ReanimationTryToGet(mCrazyDaveReanimID) == null);
			Reanimation reanimation = AddReanimation(0f, 0f, 0, ReanimationType.REANIM_CRAZY_DAVE);
			reanimation.mIsAttachment = true;
			reanimation.SetBasePoseFromAnim(GlobalMembersReanimIds.ReanimTrackId_anim_idle_handing);
			mCrazyDaveReanimID = ReanimationGetID(reanimation);
			reanimation.PlayReanim(GlobalMembersReanimIds.ReanimTrackId_anim_enter, ReanimLoopType.REANIM_PLAY_ONCE_AND_HOLD, 0, 24f);
			mCrazyDaveState = CrazyDaveState.CRAZY_DAVE_ENTERING;
			mCrazyDaveMessageIndex = -1;
			mCrazyDaveMessageText = string.Empty;
			mCrazyDaveBlinkCounter = TodCommon.RandRangeInt(400, 800);
			if (mGameScene == GameScenes.SCENE_LEVEL_INTRO && IsStormyNightLevel())
			{
				reanimation.mColorOverride = new SexyColor(64, 64, 64);
			}
		}

		public void FinishZenGardenTutorial()
		{
			mZenGarden.mIsTutorial = false;
			mPlayerInfo.mZenGardenTutorialComplete = true;
			mPlayerInfo.mIsInZenTutorial = false;
			mBoardResult = BoardResult.BOARDRESULT_WON;
			KillBoard();
			PreNewGame(GameMode.GAMEMODE_ADVENTURE, false);
		}

		public void UpdateCrazyDave()
		{
			Reanimation reanimation = ReanimationTryToGet(mCrazyDaveReanimID);
			if (reanimation == null)
			{
				return;
			}
			if (mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_ENTERING || mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_TALKING)
			{
				if (reanimation.mLoopCount > 0)
				{
					reanimation.PlayReanim(GlobalMembersReanimIds.ReanimTrackId_anim_idle, ReanimLoopType.REANIM_LOOP, 20, 12f);
					mCrazyDaveState = CrazyDaveState.CRAZY_DAVE_IDLING;
				}
			}
			else if (mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_HANDING_TALKING)
			{
				if (reanimation.mLoopCount > 0)
				{
					reanimation.PlayReanim(GlobalMembersReanimIds.ReanimTrackId_anim_idle_handing, ReanimLoopType.REANIM_LOOP, 20, 12f);
					mCrazyDaveState = CrazyDaveState.CRAZY_DAVE_HANDING_IDLING;
				}
			}
			else if (mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_LEAVING && reanimation.mLoopCount > 0)
			{
				CrazyDaveDie();
			}
			if (mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_IDLING || mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_HANDING_IDLING)
			{
				if (mCrazyDaveMessageText.IndexOf("{MOUTH_BIG_SMILE}") != -1)
				{
					reanimation.SetImageOverride(GlobalMembersReanimIds.ReanimTrackId_dave_mouths, AtlasResources.IMAGE_REANIM_CRAZYDAVE_MOUTH1);
					mCrazyDaveMessageText = mCrazyDaveMessageText.Replace("{MOUTH_BIG_SMILE}", "");
				}
				else if (mCrazyDaveMessageText.IndexOf("{MOUTH_SMALL_SMILE}") != -1)
				{
					reanimation.SetImageOverride(GlobalMembersReanimIds.ReanimTrackId_dave_mouths, AtlasResources.IMAGE_REANIM_CRAZYDAVE_MOUTH5);
					mCrazyDaveMessageText = mCrazyDaveMessageText.Replace("{MOUTH_SMALL_SMILE}", "");
				}
				else if (mCrazyDaveMessageText.IndexOf("{MOUTH_BIG_OH}") != -1)
				{
					reanimation.SetImageOverride(GlobalMembersReanimIds.ReanimTrackId_dave_mouths, AtlasResources.IMAGE_REANIM_CRAZYDAVE_MOUTH4);
					mCrazyDaveMessageText = mCrazyDaveMessageText.Replace("{MOUTH_BIG_OH}", "");
				}
				else if (mCrazyDaveMessageText.IndexOf("{MOUTH_SMALL_OH}") != -1)
				{
					reanimation.SetImageOverride(GlobalMembersReanimIds.ReanimTrackId_dave_mouths, AtlasResources.IMAGE_REANIM_CRAZYDAVE_MOUTH6);
					mCrazyDaveMessageText = mCrazyDaveMessageText.Replace("{MOUTH_SMALL_OH}", "");
				}
			}
			Reanimation reanimation2;
			if (mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_IDLING || mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_TALKING || mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_HANDING_TALKING || mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_HANDING_IDLING)
			{
				mCrazyDaveBlinkCounter--;
				if (mCrazyDaveBlinkCounter <= 0)
				{
					mCrazyDaveBlinkCounter = TodCommon.RandRangeInt(400, 800);
					reanimation2 = AddReanimation(0f, 0f, 0, ReanimationType.REANIM_CRAZY_DAVE);
					reanimation2.SetFramesForLayer(GlobalMembersReanimIds.ReanimTrackId_anim_blink);
					reanimation2.mLoopType = ReanimLoopType.REANIM_PLAY_ONCE_FULL_LAST_FRAME_AND_HOLD;
					reanimation2.mAnimRate = 15f;
					reanimation2.AttachToAnotherReanimation(ref reanimation, GlobalMembersReanimIds.ReanimTrackId_dave_head);
					reanimation2.mColorOverride = reanimation.mColorOverride;
					reanimation.AssignRenderGroupToTrack(GlobalMembersReanimIds.ReanimTrackId_dave_eye, -1);
					mCrazyDaveBlinkReanimID = ReanimationGetID(reanimation2);
				}
			}
			reanimation2 = ReanimationTryToGet(mCrazyDaveBlinkReanimID);
			if (reanimation2 != null && reanimation2.mLoopCount > 0)
			{
				reanimation.AssignRenderGroupToTrack(GlobalMembersReanimIds.ReanimTrackId_dave_eye, 0);
				RemoveReanimation(ref mCrazyDaveBlinkReanimID);
				mCrazyDaveBlinkReanimID = null;
			}
			reanimation.Update();
		}

		public void CrazyDaveTalkIndex(int theMessageIndex)
		{
			mCrazyDaveMessageIndex = theMessageIndex;
			string crazyDaveText = GetCrazyDaveText(theMessageIndex);
			CrazyDaveTalkMessage(crazyDaveText);
		}

		public void CrazyDaveTalkMessage(string theMessage)
		{
			Reanimation reanimation = ReanimationGet(mCrazyDaveReanimID);
			bool flag = false;
			if (theMessage.IndexOf("{HANDING}") != -1)
			{
				flag = true;
				theMessage = theMessage.Replace("{HANDING}", string.Empty);
			}
			if ((mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_HANDING_TALKING || mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_HANDING_IDLING) && !flag)
			{
				CrazyDaveDoneHanding();
			}
			bool flag2 = true;
			if (theMessage.IndexOf("{NO_SOUND}") != -1)
			{
				flag2 = false;
				theMessage = theMessage.Replace("{NO_SOUND}", "");
			}
			else
			{
				CrazyDaveStopSound();
			}
			int num = 0;
			bool flag3 = false;
			for (int i = 0; i < theMessage.length(); i++)
			{
				if (theMessage[i] == '{')
				{
					flag3 = true;
				}
				else if (theMessage[i] == '}')
				{
					flag3 = false;
				}
				else if (!flag3)
				{
					num++;
				}
			}
			Image theImage = null;
			reanimation.SetImageOverride(GlobalMembersReanimIds.ReanimTrackId_dave_mouths, theImage);
			if (mCrazyDaveState != CrazyDaveState.CRAZY_DAVE_TALKING || flag2)
			{
				if (flag)
				{
					reanimation.PlayReanim(GlobalMembersReanimIds.ReanimTrackId_anim_talk_handing, ReanimLoopType.REANIM_LOOP, 50, 12f);
					if (flag2 && theMessage.IndexOf("{SHORT_SOUND}") != -1)
					{
						PlayFoley(FoleyType.FOLEY_CRAZYDAVESHORT);
						theMessage = theMessage.Replace("{SHORT_SOUND}", "");
					}
					else if (flag2 && theMessage.IndexOf("{SCREAM}") != -1)
					{
						PlayFoley(FoleyType.FOLEY_CRAZYDAVESCREAM);
						theMessage = theMessage.Replace("{SCREAM}", "");
					}
					else if (flag2)
					{
						PlayFoley(FoleyType.FOLEY_CRAZYDAVELONG);
					}
					mCrazyDaveState = CrazyDaveState.CRAZY_DAVE_HANDING_TALKING;
				}
				else if (theMessage.IndexOf("{SHAKE}") != -1)
				{
					reanimation.PlayReanim(GlobalMembersReanimIds.ReanimTrackId_anim_crazy, ReanimLoopType.REANIM_PLAY_ONCE_AND_HOLD, 50, 12f);
					if (flag2)
					{
						PlayFoley(FoleyType.FOLEY_CRAZYDAVECRAZY);
					}
					mCrazyDaveState = CrazyDaveState.CRAZY_DAVE_TALKING;
					theMessage = theMessage.Replace("{SHAKE}", "");
				}
				else if (theMessage.IndexOf("{SCREAM}") != -1)
				{
					reanimation.PlayReanim(GlobalMembersReanimIds.ReanimTrackId_anim_smalltalk, ReanimLoopType.REANIM_PLAY_ONCE_AND_HOLD, 50, 12f);
					if (flag2)
					{
						PlayFoley(FoleyType.FOLEY_CRAZYDAVESCREAM);
					}
					mCrazyDaveState = CrazyDaveState.CRAZY_DAVE_TALKING;
					theMessage = theMessage.Replace("{SCREAM}", "");
				}
				else if (theMessage.IndexOf("{SCREAM2}") != -1)
				{
					reanimation.PlayReanim(GlobalMembersReanimIds.ReanimTrackId_anim_mediumtalk, ReanimLoopType.REANIM_PLAY_ONCE_AND_HOLD, 50, 12f);
					if (flag2)
					{
						PlayFoley(FoleyType.FOLEY_CRAZYDAVESCREAM2);
					}
					mCrazyDaveState = CrazyDaveState.CRAZY_DAVE_TALKING;
					theMessage = theMessage.Replace("{SCREAM2}", "");
				}
				else if (theMessage.IndexOf("{SHOW_WALLNUT}") != -1)
				{
					reanimation.PlayReanim(GlobalMembersReanimIds.ReanimTrackId_anim_talk_handing, ReanimLoopType.REANIM_LOOP, 50, 12f);
					Reanimation reanimation2 = AddReanimation(0f, 0f, 0, ReanimationType.REANIM_WALLNUT);
					reanimation2.PlayReanim(GlobalMembersReanimIds.ReanimTrackId_anim_idle, ReanimLoopType.REANIM_LOOP, 0, 12f);
					ReanimatorTrackInstance trackInstanceByName = reanimation.GetTrackInstanceByName(GlobalMembersReanimIds.ReanimTrackId_dave_handinghand);
					AttachEffect attachEffect = GlobalMembersAttachment.AttachReanim(ref trackInstanceByName.mAttachmentID, reanimation2, 100f * Constants.S, 393f * Constants.S);
					attachEffect.mOffset.mMatrix.M11 = 1.2f;
					attachEffect.mOffset.mMatrix.M22 = 1.2f;
					reanimation.Update();
					if (flag2)
					{
						PlayFoley(FoleyType.FOLEY_CRAZYDAVESCREAM2);
					}
					mCrazyDaveState = CrazyDaveState.CRAZY_DAVE_HANDING_TALKING;
					theMessage = theMessage.Replace("{SHOW_WALLNUT}", "");
				}
				else if (theMessage.IndexOf("{SHOW_HAMMER}") != -1)
				{
					reanimation.PlayReanim(GlobalMembersReanimIds.ReanimTrackId_anim_talk_handing, ReanimLoopType.REANIM_LOOP, 50, 12f);
					Reanimation reanimation3 = AddReanimation(0f, 0f, 0, ReanimationType.REANIM_HAMMER);
					reanimation3.PlayReanim(GlobalMembersReanimIds.ReanimTrackId_anim_whack_zombie, ReanimLoopType.REANIM_PLAY_ONCE_AND_HOLD, 0, 24f);
					reanimation3.mAnimTime = 1f;
					ReanimatorTrackInstance trackInstanceByName2 = reanimation.GetTrackInstanceByName(GlobalMembersReanimIds.ReanimTrackId_dave_handinghand);
					AttachEffect attachEffect2 = GlobalMembersAttachment.AttachReanim(ref trackInstanceByName2.mAttachmentID, reanimation3, 62f * Constants.S, 445f * Constants.S);
					attachEffect2.mOffset.mMatrix.M11 = 1.5f;
					attachEffect2.mOffset.mMatrix.M22 = 1.5f;
					reanimation.Update();
					if (flag2)
					{
						PlayFoley(FoleyType.FOLEY_CRAZYDAVELONG);
					}
					mCrazyDaveState = CrazyDaveState.CRAZY_DAVE_HANDING_TALKING;
					theMessage = theMessage.Replace("{SHOW_HAMMER}", "");
				}
				else if (num < 23)
				{
					reanimation.PlayReanim(GlobalMembersReanimIds.ReanimTrackId_anim_smalltalk, ReanimLoopType.REANIM_PLAY_ONCE_AND_HOLD, 50, 12f);
					if (flag2)
					{
						PlayFoley(FoleyType.FOLEY_CRAZYDAVESHORT);
					}
					mCrazyDaveState = CrazyDaveState.CRAZY_DAVE_TALKING;
				}
				else if (num < 52)
				{
					reanimation.PlayReanim(GlobalMembersReanimIds.ReanimTrackId_anim_mediumtalk, ReanimLoopType.REANIM_PLAY_ONCE_AND_HOLD, 50, 12f);
					if (flag2)
					{
						PlayFoley(FoleyType.FOLEY_CRAZYDAVELONG);
					}
					mCrazyDaveState = CrazyDaveState.CRAZY_DAVE_TALKING;
				}
				else
				{
					reanimation.PlayReanim(GlobalMembersReanimIds.ReanimTrackId_anim_blahblah, ReanimLoopType.REANIM_PLAY_ONCE_AND_HOLD, 50, 12f);
					if (flag2)
					{
						PlayFoley(FoleyType.FOLEY_CRAZYDAVEEXTRALONG);
					}
					mCrazyDaveState = CrazyDaveState.CRAZY_DAVE_TALKING;
				}
			}
			mCrazyDaveMessageText = theMessage;
		}

		public void CrazyDaveLeave()
		{
			Reanimation reanimation = ReanimationTryToGet(mCrazyDaveReanimID);
			if (reanimation == null)
			{
				return;
			}
			if (mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_HANDING_TALKING || mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_HANDING_IDLING)
			{
				CrazyDaveDoneHanding();
			}
			reanimation.PlayReanim(GlobalMembersReanimIds.ReanimTrackId_anim_leave, ReanimLoopType.REANIM_PLAY_ONCE_AND_HOLD, 20, 24f);
			Image theImage = null;
			reanimation.SetImageOverride(GlobalMembersReanimIds.ReanimTrackId_dave_mouths, theImage);
			mCrazyDaveState = CrazyDaveState.CRAZY_DAVE_LEAVING;
			mCrazyDaveMessageIndex = -1;
			mCrazyDaveMessageText = string.Empty;
			CrazyDaveStopSound();
		}

		public void DrawCrazyDave(Graphics g)
		{
			DrawCrazyDave(g, false);
		}

		public void DrawCrazyDave(Graphics g, bool theUseSmallFont)
		{
			Reanimation reanimation = ReanimationTryToGet(mCrazyDaveReanimID);
			if (reanimation == null)
			{
				return;
			}
			int theWidth = Constants.RetardedDave_Bubble_Size;
			if (!mCrazyDaveMessageText.empty())
			{
				int num = (int)Constants.InvertAndScale(285f);
				int num2 = (int)Constants.InvertAndScale(80f);
				Image image_STORE_SPEECHBUBBLE = AtlasResources.IMAGE_STORE_SPEECHBUBBLE;
				if (base.GetDialog(Dialogs.DIALOG_STORE) != null)
				{
					num += Constants.RetardedDave_Bubble_Offset_Shop.X;
					num2 += Constants.RetardedDave_Bubble_Offset_Shop.Y;
					theWidth = (int)Constants.InvertAndScale(150f);
					int num3 = (int)Constants.InvertAndScale(105f);
					g.DrawImage(image_STORE_SPEECHBUBBLE, num, num2, new TRect(0, 0, (int)Constants.InvertAndScale(64f), image_STORE_SPEECHBUBBLE.mHeight));
					g.DrawImage(image_STORE_SPEECHBUBBLE, num + (int)Constants.InvertAndScale(64f), num2, new TRect(image_STORE_SPEECHBUBBLE.mWidth - num3, 0, num3, image_STORE_SPEECHBUBBLE.mHeight));
				}
				else if (mGameMode == GameMode.GAMEMODE_CHALLENGE_ZEN_GARDEN)
				{
					num += Constants.ZenGarden_RetardedDaveBubble_Pos.X;
					num2 += Constants.ZenGarden_RetardedDaveBubble_Pos.Y;
					g.DrawImage(image_STORE_SPEECHBUBBLE, num, num2);
				}
				else
				{
					num += Constants.RetardedDave_Bubble_Offset_Board.X;
					num2 += Constants.RetardedDave_Bubble_Offset_Board.Y;
					g.DrawImage(image_STORE_SPEECHBUBBLE, num, num2);
				}
				g.DrawImage(AtlasResources.IMAGE_STORE_SPEECHBUBBLE_TIP, num + (int)Constants.InvertAndScale(30f), num2 - Constants.RetardedDave_Bubble_Tip_Offset + image_STORE_SPEECHBUBBLE.mHeight);
				string text = mCrazyDaveMessageText;
				TRect theRect = new TRect(num + Constants.RetardedDave_Bubble_Rect.mX, num2 + Constants.RetardedDave_Bubble_Rect.mY, theWidth, Constants.RetardedDave_Bubble_Rect.mHeight);
				int x = theRect.mX;
				if (text.IndexOf("{SHAKE}") != -1)
				{
					text = TodCommon.TodReplaceString(text, "{SHAKE}", "");
					theRect.mX += RandomNumbers.NextNumber() % 2;
					theRect.mY += RandomNumbers.NextNumber() % 2;
				}
				bool flag = true;
				if (mGameMode == GameMode.GAMEMODE_UPSELL)
				{
					flag = false;
				}
				else if (text.IndexOf("{NO_CLICK}") != -1)
				{
					string text2;
					if (!LawnApp.noClickStringCache.TryGetValue(text, out text2))
					{
						text2 = TodCommon.TodReplaceString(text, "{NO_CLICK}", string.Empty);
						LawnApp.noClickStringCache.Add(text, text2);
					}
					text = text2;
					flag = false;
				}
				g.SetColor(SexyColor.Black);
				g.SetFont(theUseSmallFont ? Resources.FONT_BRIANNETOD12 : Resources.FONT_BRIANNETOD16);
				TodStringFile.TodDrawStringWrapped(g, text, theRect, theUseSmallFont ? Resources.FONT_BRIANNETOD12 : Resources.FONT_BRIANNETOD16, SexyColor.Black, DrawStringJustification.DS_ALIGN_CENTER_VERTICAL_MIDDLE);
				if (flag)
				{
					TodCommon.TodDrawString(g, "[TAP_TO_CONTINUE]", x + theRect.mWidth / 2, num2 + Constants.RetardedDave_Bubble_TapToContinue_Y, Resources.FONT_PICO129, SexyColor.Black, DrawStringJustification.DS_ALIGN_CENTER);
				}
			}
			reanimation.Draw(g);
		}

		public void CrazyDaveDie()
		{
			Reanimation reanimation = ReanimationTryToGet(mCrazyDaveReanimID);
			if (reanimation == null)
			{
				return;
			}
			reanimation.ReanimationDie();
			mCrazyDaveState = CrazyDaveState.CRAZY_DAVE_OFF;
			mCrazyDaveReanimID = null;
			mCrazyDaveMessageIndex = -1;
			mCrazyDaveMessageText = string.Empty;
			CrazyDaveStopSound();
		}

		public void DoUpsellScreen()
		{
		}

		public void CrazyDaveStopTalking()
		{
			bool flag = true;
			if (mGameMode == GameMode.GAMEMODE_UPSELL)
			{
				flag = false;
			}
			if (flag && mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_HANDING_TALKING)
			{
				CrazyDaveDoneHanding();
			}
			Image theImage = null;
			Reanimation reanimation = ReanimationGet(mCrazyDaveReanimID);
			reanimation.SetImageOverride(GlobalMembersReanimIds.ReanimTrackId_dave_mouths, theImage);
			if (mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_HANDING_TALKING && !flag)
			{
				reanimation.PlayReanim(GlobalMembersReanimIds.ReanimTrackId_anim_idle_handing, ReanimLoopType.REANIM_LOOP, 20, 12f);
				mCrazyDaveState = CrazyDaveState.CRAZY_DAVE_HANDING_IDLING;
			}
			else if (mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_TALKING || mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_HANDING_TALKING)
			{
				reanimation.PlayReanim(GlobalMembersReanimIds.ReanimTrackId_anim_idle, ReanimLoopType.REANIM_LOOP, 20, 12f);
				mCrazyDaveState = CrazyDaveState.CRAZY_DAVE_IDLING;
			}
			mCrazyDaveMessageIndex = -1;
			mCrazyDaveMessageText = string.Empty;
			CrazyDaveStopSound();
		}

		public void PreloadForUser()
		{
			int num = mCompletedLoadingThreadTasks + GetNumPreloadingTasks();
			if (mTitleScreen != null && mTitleScreen.mQuickLoadKey != KeyCode.KEYCODE_UNKNOWN)
			{
				mCompletedLoadingThreadTasks = num;
				return;
			}
			ReanimatorXnaHelpers.ReanimatorEnsureDefinitionLoaded(ReanimationType.REANIM_PUFF, true);
			ReanimatorXnaHelpers.ReanimatorEnsureDefinitionLoaded(ReanimationType.REANIM_LAWN_MOWERED_ZOMBIE, true);
			ReanimatorXnaHelpers.ReanimatorEnsureDefinitionLoaded(ReanimationType.REANIM_READYSETPLANT, true);
			mCompletedLoadingThreadTasks += 68;
			ReanimatorXnaHelpers.ReanimatorEnsureDefinitionLoaded(ReanimationType.REANIM_FINAL_WAVE, true);
			ReanimatorXnaHelpers.ReanimatorEnsureDefinitionLoaded(ReanimationType.REANIM_SUN, true);
			ReanimatorXnaHelpers.ReanimatorEnsureDefinitionLoaded(ReanimationType.REANIM_TEXT_FADE_ON, true);
			mCompletedLoadingThreadTasks += 68;
			ReanimatorXnaHelpers.ReanimatorEnsureDefinitionLoaded(ReanimationType.REANIM_ZOMBIE, true);
			mCompletedLoadingThreadTasks += 68;
			ReanimatorXnaHelpers.ReanimatorEnsureDefinitionLoaded(ReanimationType.REANIM_ZOMBIE_NEWSPAPER, true);
			mCompletedLoadingThreadTasks += 68;
			ReanimatorXnaHelpers.ReanimatorEnsureDefinitionLoaded(ReanimationType.REANIM_SELECTOR_SCREEN, true);
			mCompletedLoadingThreadTasks += 340;
			mCompletedLoadingThreadTasks += 68;
			if (mPlayerInfo != null)
			{
				for (int i = 0; i < 53; i++)
				{
					SeedType theSeedType = (SeedType)i;
					if (HasSeedType(theSeedType) || HasFinishedAdventure())
					{
						Plant.PreloadPlantResources(theSeedType);
						if (mCompletedLoadingThreadTasks < num)
						{
							mCompletedLoadingThreadTasks += 68;
						}
						if (mTitleScreen != null && mTitleScreen.mQuickLoadKey != KeyCode.KEYCODE_UNKNOWN)
						{
							mCompletedLoadingThreadTasks = num;
							return;
						}
						if (mShutdown || mCloseRequest)
						{
							return;
						}
					}
				}
				int j = 0;
				while (j < 33)
				{
					ZombieType zombieType = (ZombieType)j;
					if (HasFinishedAdventure())
					{
						goto IL_175;
					}
					ZombieDefinition zombieDefinition = Zombie.GetZombieDefinition(zombieType);
					if (mPlayerInfo.mLevel >= zombieDefinition.mStartingLevel)
					{
						goto IL_175;
					}
					IL_1E0:
					j++;
					continue;
					IL_175:
					if (zombieType == ZombieType.ZOMBIE_BOSS || zombieType == ZombieType.ZOMBIE_CATAPULT || zombieType == ZombieType.ZOMBIE_GARGANTUAR || zombieType == ZombieType.ZOMBIE_DIGGER || zombieType == ZombieType.ZOMBIE_ZAMBONI)
					{
						goto IL_1E0;
					}
					Zombie.PreloadZombieResources(zombieType);
					if (mCompletedLoadingThreadTasks < num)
					{
						mCompletedLoadingThreadTasks += 68;
					}
					if (mTitleScreen != null && mTitleScreen.mQuickLoadKey != KeyCode.KEYCODE_UNKNOWN)
					{
						mCompletedLoadingThreadTasks = num;
						return;
					}
					if (mShutdown || mCloseRequest)
					{
						return;
					}
					goto IL_1E0;
				}
			}
			if (mCompletedLoadingThreadTasks != num)
			{
				mCompletedLoadingThreadTasks = num;
			}
		}

		public void PreloadLoadingThreadReanimations()
		{
		}

		public void PreloadReanimation(ReanimationType theReanimType)
		{
		}

		public int GetNumPreloadingTasks()
		{
			int num = 10;
			if (mPlayerInfo != null)
			{
				for (int i = 0; i < 53; i++)
				{
					SeedType theSeedType = (SeedType)i;
					if (HasSeedType(theSeedType) || HasFinishedAdventure())
					{
						num++;
					}
				}
				int j = 0;
				while (j < 33)
				{
					ZombieType zombieType = (ZombieType)j;
					if (HasFinishedAdventure())
					{
						goto IL_5B;
					}
					ZombieDefinition zombieDefinition = Zombie.GetZombieDefinition(zombieType);
					if (mPlayerInfo.mLevel >= zombieDefinition.mStartingLevel)
					{
						goto IL_5B;
					}
					IL_7D:
					j++;
					continue;
					IL_5B:
					if (zombieType != ZombieType.ZOMBIE_BOSS && zombieType != ZombieType.ZOMBIE_CATAPULT && zombieType != ZombieType.ZOMBIE_GARGANTUAR && zombieType != ZombieType.ZOMBIE_DIGGER && zombieType != ZombieType.ZOMBIE_ZAMBONI)
					{
						num++;
						goto IL_7D;
					}
					goto IL_7D;
				}
			}
			return num * 68;
		}

		public void LawnMessageBox(int theDialogId, string theHeaderName, string theLinesName, string theButton1Name, string theButton2Name, int theButtonMode, LawnMessageBoxListener theListener)
		{
			mOldFocus = mWidgetManager.mFocusWidget;
			mLawnMessageBoxListener = theListener;
			LawnDialog lawnDialog = DoDialog(theDialogId, true, theHeaderName, theLinesName, theButton1Name, theButtonMode);
			if (lawnDialog.mLawnYesButton != null)
			{
				lawnDialog.mLawnYesButton.mLabel = TodStringFile.TodStringTranslate(theButton1Name);
			}
			if (lawnDialog.mLawnNoButton != null)
			{
				lawnDialog.mLawnNoButton.mLabel = TodStringFile.TodStringTranslate(theButton2Name);
			}
			lawnDialog.CalcSize(0, 0, (int)Constants.InvertAndScale(400f));
			mWidgetManager.SetFocus(lawnDialog);
		}

		public virtual void EnforceCursor()
		{
		}

		public void ShowCreditScreen()
		{
			mCreditScreen = new CreditScreen(this);
			mCreditScreen.Resize(0, 0, mWidth, mHeight);
			mWidgetManager.AddWidget(mCreditScreen);
			mWidgetManager.BringToBack(mCreditScreen);
			mWidgetManager.SetFocus(mCreditScreen);
		}

		public void KillCreditScreen()
		{
			if (mCreditScreen != null)
			{
				mWidgetManager.RemoveWidget(mCreditScreen);
				base.SafeDeleteWidget(mCreditScreen);
				mCreditScreen = null;
			}
		}

		public string Pluralize(int theCount, string theSingular, string thePlural)
		{
			if (theCount == 1)
			{
				return TodCommon.TodReplaceNumberString(theSingular, "{COUNT}", theCount);
			}
			return TodCommon.TodReplaceNumberString(thePlural, "{COUNT}", theCount);
		}

		public int GetNumTrophies(ChallengePage thePage)
		{
			int num = 0;
			for (int i = 1; i < 69; i++)
			{
				ChallengeDefinition challengeDefinition = ChallengeScreen.gChallengeDefs[i - 1];
				if (thePage == challengeDefinition.mPage && HasBeatenChallenge(challengeDefinition.mChallengeMode))
				{
					num++;
				}
			}
			return num;
		}

		public bool EarnedGoldTrophy()
		{
			return HasFinishedAdventure() && TrophiesNeedForGoldSunflower() <= 0;
		}

		public bool IsRegistered()
		{
			return false;
		}

		public bool IsTrialStageLocked()
		{
			return false;
		}

		public bool IsDRMConnected()
		{
			return false;
		}

		public bool IsScaryPotterLevel()
		{
			return (mGameMode >= GameMode.GAMEMODE_SCARY_POTTER_1 && mGameMode <= GameMode.GAMEMODE_SCARY_POTTER_ENDLESS) || ((IsAdventureMode() && mPlayerInfo.mLevel == 35) || mGameMode == GameMode.GAMEMODE_QUICKPLAY_35);
		}

		public bool IsEndlessScaryPotter(GameMode theGameMode)
		{
			return theGameMode == GameMode.GAMEMODE_SCARY_POTTER_ENDLESS;
		}

		public bool IsSquirrelLevel()
		{
			return mBoard != null && mGameMode == GameMode.GAMEMODE_CHALLENGE_SQUIRREL;
		}

		public bool IsIZombieLevel()
		{
			return mBoard != null && (mGameMode == GameMode.GAMEMODE_PUZZLE_I_ZOMBIE_1 || mGameMode == GameMode.GAMEMODE_PUZZLE_I_ZOMBIE_2 || mGameMode == GameMode.GAMEMODE_PUZZLE_I_ZOMBIE_3 || mGameMode == GameMode.GAMEMODE_PUZZLE_I_ZOMBIE_4 || mGameMode == GameMode.GAMEMODE_PUZZLE_I_ZOMBIE_5 || mGameMode == GameMode.GAMEMODE_PUZZLE_I_ZOMBIE_6 || mGameMode == GameMode.GAMEMODE_PUZZLE_I_ZOMBIE_7 || mGameMode == GameMode.GAMEMODE_PUZZLE_I_ZOMBIE_8 || mGameMode == GameMode.GAMEMODE_PUZZLE_I_ZOMBIE_9 || mGameMode == GameMode.GAMEMODE_PUZZLE_I_ZOMBIE_ENDLESS);
		}

		public bool CanShowZenGarden()
		{
			return mPlayerInfo != null && !IsTrialStageLocked() && (HasFinishedAdventure() || mPlayerInfo.mLevel >= 45);
		}

		public static string GetMoneyString(int theAmount)
		{
			int theValue = theAmount * 10;
			string text;
			if (!LawnApp.moneyStrings.TryGetValue(theAmount, out text))
			{
				text = TodCommon.TodReplaceString(TodStringFile.TodStringTranslate("[CURRENCY_STRING]"), "{CURRENCY_SYMBOL}", TodStringFile.TodStringTranslate("[CURRENCY_SYMBOL]"));
				text = TodCommon.TodReplaceString(text, "{AMOUNT}", Common.CommaSeperate(theValue));
				LawnApp.moneyStrings.Add(theAmount, text);
			}
			return text;
		}

		public static string ToString(int i)
		{
			string text;
			if (!LawnApp.cachedIntToString.TryGetValue(i, out text))
			{
				text = i.ToString();
				LawnApp.cachedIntToString.Add(i, text);
			}
			return text;
		}

		public bool AdvanceCrazyDaveText()
		{
			int num = mCrazyDaveMessageIndex + 1;
			string theString = Common.StrFormat_("[CRAZY_DAVE_{0}]", num);
			if (!TodStringFile.TodStringListExists(theString))
			{
				return false;
			}
			CrazyDaveTalkIndex(num);
			return true;
		}

		public bool IsWhackAZombieLevel()
		{
			return mBoard != null && (mGameMode == GameMode.GAMEMODE_CHALLENGE_WHACK_A_ZOMBIE || ((IsAdventureMode() && mPlayerInfo.mLevel == 15) || mGameMode == GameMode.GAMEMODE_QUICKPLAY_15));
		}

		public void UpdatePlayTimeStats()
		{
		}

		public bool CanPauseNow()
		{
			return mBoard != null && (mSeedChooserScreen == null || !mSeedChooserScreen.mMouseVisible) && mBoard.mBoardFadeOutCounter < 0 && mCrazyDaveState == CrazyDaveState.CRAZY_DAVE_OFF && mGameMode != GameMode.GAMEMODE_CHALLENGE_ZEN_GARDEN && mGameMode != GameMode.GAMEMODE_TREE_OF_WISDOM && base.GetDialogCount() <= 0;
		}

		public bool IsPuzzleMode()
		{
			return (mGameMode >= GameMode.GAMEMODE_SCARY_POTTER_1 && mGameMode <= GameMode.GAMEMODE_SCARY_POTTER_ENDLESS) || (mGameMode >= GameMode.GAMEMODE_PUZZLE_I_ZOMBIE_1 && mGameMode <= GameMode.GAMEMODE_PUZZLE_I_ZOMBIE_ENDLESS);
		}

		public bool IsChallengeMode()
		{
			return !IsAdventureMode() && !IsQuickPlayMode() && !IsPuzzleMode() && !IsSurvivalMode();
		}

		public bool IsEndlessIZombie(GameMode theGameMode)
		{
			return theGameMode == GameMode.GAMEMODE_PUZZLE_I_ZOMBIE_ENDLESS;
		}

		public void CrazyDaveDoneHanding()
		{
			Reanimation reanimation = ReanimationGet(mCrazyDaveReanimID);
			ReanimatorTrackInstance trackInstanceByName = reanimation.GetTrackInstanceByName(GlobalMembersReanimIds.ReanimTrackId_dave_handinghand);
			GlobalMembersAttachment.AttachmentDie(ref trackInstanceByName.mAttachmentID);
		}

		public int TrophiesNeedForGoldSunflower()
		{
			return 0;
		}

		public int GetCurrentChallengeIndex()
		{
			return mGameMode - GameMode.GAMEMODE_SURVIVAL_NORMAL_STAGE_1;
		}

		public void LoadGroup(string theGroupName, int theGroupAveMsToLoad)
		{
			PerfTimer perfTimer = default(PerfTimer);
			perfTimer.Start();
			mResourceManager.StartLoadResources(theGroupName);
			while (!mShutdown && !mCloseRequest && !mLoadingFailed && TodCommon.TodLoadNextResource())
			{
				mCompletedLoadingThreadTasks += theGroupAveMsToLoad;
			}
			if (mShutdown || mCloseRequest)
			{
				return;
			}
			if (mResourceManager.HadError())
			{
				ShowResourceError();
				mLoadingFailed = true;
				return;
			}
			if (!Resources.ExtractResourcesByName(mResourceManager, theGroupName))
			{
				ShowResourceError();
				mLoadingFailed = true;
				return;
			}
			int theTotalGroupWeigth = mResourceManager.GetNumResources(theGroupName) * theGroupAveMsToLoad;
			int theGroupTime = Math.Max((int)perfTimer.GetDuration(), 0);
			TraceLoadGroup(theGroupName, theGroupTime, theTotalGroupWeigth, theGroupAveMsToLoad);
		}

		public void TraceLoadGroup(string theGroupName, int theGroupTime, int theTotalGroupWeigth, int theTaskWeight)
		{
		}

		public void DelayLoadBackgroundResource(string theGroupName)
		{
			if (mLastBackgroundResGroupLoaded != theGroupName)
			{
				if (!string.IsNullOrEmpty(mLastBackgroundResGroupLoaded))
				{
					mResourceManager.UnloadBackground(mLastBackgroundResGroupLoaded);
				}
				if (!string.IsNullOrEmpty(theGroupName))
				{
					TodCommon.TodLoadResources(theGroupName);
				}
				mLastBackgroundResGroupLoaded = theGroupName;
			}
		}

		private static void LoadResourceInThread(string resourceGroup)
		{
			if (Main.LOW_MEMORY_DEVICE)
			{
				Thread thread = new Thread(new ParameterizedThreadStart(LawnApp.Load));
				LoadingScreen.IsLoading = true;
				thread.IsBackground = true;
				thread.Start(resourceGroup);
				return;
			}
			LawnApp.Load(resourceGroup);
		}

		private static void Load(object resourceGroup)
		{
			string theGroup = resourceGroup as string;
			TodCommon.TodLoadResources(theGroup);
		}

		public void DelayLoadLeaderboardResource(bool doLoad)
		{
			if (!Main.LOW_MEMORY_DEVICE && leaderboardLoaded)
			{
				return;
			}
			if (doLoad)
			{
				DelayLoadMainMenuResource(false);
				DelayLoadGamePlayResources(false);
			}
			if (doLoad && !leaderboardLoaded)
			{
				LawnApp.LoadResourceInThread("DelayLoad_Leaderboard");
			}
			else if (leaderboardLoaded && !doLoad)
			{
				mResourceManager.UnloadBackground("DelayLoad_Leaderboard");
			}
			DelayLoadPileResource(doLoad);
			if (!doLoad)
			{
				DelayLoadMainMenuResource(true);
			}
			leaderboardLoaded = doLoad;
		}

		public void DelayLoadGamePlayResources(bool doLoad)
		{
			if (!Main.LOW_MEMORY_DEVICE && gamePlayLoaded)
			{
				return;
			}
			if (doLoad && !gamePlayLoaded)
			{
				GC.Collect();
				TodCommon.TodLoadResources("DelayLoad_GamePlay");
				AtlasResources.mAtlasResources.UnpackPlantsZombiesAtlasImages();
				AtlasResources.mAtlasResources.UnpackParticlesAtlasImages();
				if (Main.LOW_MEMORY_DEVICE)
				{
					ResourceManager.mReanimContentManager.Unload();
					ReanimatorXnaHelpers.ReanimatorLoadDefinitions(ref GameConstants.gLawnReanimationArray, 119);
					ResourceManager.mParticleContentManager.Unload();
					TodParticleGlobal.TodParticleLoadDefinitions(ref GameConstants.gLawnParticleArray, 102);
				}
			}
			else if (gamePlayLoaded && !doLoad)
			{
				mResourceManager.UnloadBackground("DelayLoad_GamePlay");
			}
			gamePlayLoaded = doLoad;
		}

		private void DelayLoadCachedResources(bool doLoad)
		{
			if (!Main.LOW_MEMORY_DEVICE && cachedLoaded)
			{
				return;
			}
			if (doLoad && !cachedLoaded)
			{
				GC.Collect();
				TodCommon.TodLoadResources("DelayLoad_Cached");
				AtlasResources.mAtlasResources.UnpackCachedAtlasImages();
			}
			else if (cachedLoaded && !doLoad)
			{
				mResourceManager.UnloadBackground("DelayLoad_Cached");
			}
			cachedLoaded = doLoad;
		}

		public void DelayLoadZenGardenResources(bool doLoad)
		{
			if (!Main.LOW_MEMORY_DEVICE && zenGardenLoaded)
			{
				return;
			}
			if (doLoad && !zenGardenLoaded)
			{
				GC.Collect();
				TodCommon.TodLoadResources("DelayLoad_ZenGarden");
				AtlasResources.mAtlasResources.UnpackZengardenAtlasImages();
				if (Main.LOW_MEMORY_DEVICE)
				{
					ResourceManager.mReanimContentManager.Unload();
					ReanimatorXnaHelpers.ReanimatorLoadDefinitions(ref GameConstants.gLawnReanimationArray, 119);
					ResourceManager.mParticleContentManager.Unload();
					TodParticleGlobal.TodParticleLoadDefinitions(ref GameConstants.gLawnParticleArray, 102);
				}
			}
			else if (zenGardenLoaded && !doLoad)
			{
				mResourceManager.UnloadBackground("DelayLoad_ZenGarden");
			}
			zenGardenLoaded = doLoad;
		}

		public void DelayLoadMainMenuResource(bool doLoad)
		{
			if (!Main.LOW_MEMORY_DEVICE && mainMenuLoaded)
			{
				return;
			}
			if (doLoad && !mainMenuLoaded)
			{
				TodCommon.TodLoadResources("DelayLoad_MainMenu");
				AtlasResources.mAtlasResources.UnpackGoodiesAtlasImages();
				AtlasResources.mAtlasResources.UnpackQuickplayAtlasImages();
				AtlasResources.mAtlasResources.UnpackMiniGamesAtlasImages();
			}
			else if (mainMenuLoaded && !doLoad)
			{
				mResourceManager.UnloadBackground("DelayLoad_MainMenu");
			}
			mainMenuLoaded = doLoad;
		}

		public void DelayLoadPileResource(bool doLoad)
		{
			if (!Main.LOW_MEMORY_DEVICE && pileLoaded)
			{
				return;
			}
			if (doLoad && !pileLoaded)
			{
				TodCommon.TodLoadResources("DelayLoad_Pile");
				AtlasResources.mAtlasResources.UnpackPileAtlasImages();
			}
			else if (pileLoaded && !doLoad)
			{
				mResourceManager.UnloadBackground("DelayLoad_Pile");
			}
			pileLoaded = doLoad;
		}

		public void DelayLoadZombieNoteResource(string theGroupName)
		{
			if (mLastZombieNoteResGroupLoaded != theGroupName)
			{
				if (!string.IsNullOrEmpty(mLastZombieNoteResGroupLoaded))
				{
					mResourceManager.UnloadBackground(mLastZombieNoteResGroupLoaded);
				}
				TodCommon.TodLoadResources(theGroupName);
				mLastZombieNoteResGroupLoaded = theGroupName;
				Resources.ExtractResourcesByName(mResourceManager, theGroupName);
			}
		}

		public void DelayLoadZenGardenBackground(string theGroupName)
		{
			if (mLastZenGardenResourceLoaded != theGroupName)
			{
				if (!string.IsNullOrEmpty(mLastZenGardenResourceLoaded))
				{
					mResourceManager.UnloadBackground(mLastZenGardenResourceLoaded);
				}
				TodCommon.TodLoadResources(theGroupName);
				mLastZenGardenResourceLoaded = theGroupName;
				Resources.ExtractResourcesByName(mResourceManager, theGroupName);
			}
		}

		public void DelayLoadZombieNotePaperResource(string theGroupName)
		{
			if (mLastPaperGroupLoaded != theGroupName)
			{
				if (!string.IsNullOrEmpty(mLastPaperGroupLoaded))
				{
					mResourceManager.UnloadBackground(mLastPaperGroupLoaded);
				}
				TodCommon.TodLoadResources(theGroupName);
				mLastPaperGroupLoaded = theGroupName;
				Resources.ExtractResourcesByName(mResourceManager, theGroupName);
			}
		}

		public void DelayLoadUpsellResource(string theGroupName)
		{
			if (mLastStoreResGroupLoaded != theGroupName)
			{
				if (!string.IsNullOrEmpty(mLastUpsellResGroupLoaded))
				{
					mResourceManager.UnloadBackground(mLastUpsellResGroupLoaded);
				}
				TodCommon.TodLoadResources(theGroupName);
				mLastUpsellResGroupLoaded = theGroupName;
				Resources.ExtractResourcesByName(mResourceManager, theGroupName);
			}
		}

		public void DelayLoadStoreResource(string theGroupName)
		{
			if (mLastStoreResGroupLoaded != theGroupName)
			{
				if (!string.IsNullOrEmpty(mLastStoreResGroupLoaded))
				{
					mResourceManager.UnloadBackground(mLastStoreResGroupLoaded);
				}
				TodCommon.TodLoadResources(theGroupName);
				mLastStoreResGroupLoaded = theGroupName;
			}
		}

		public void CrazyDaveStopSound()
		{
			mSoundSystem.StopFoley(FoleyType.FOLEY_CRAZYDAVESHORT);
			mSoundSystem.StopFoley(FoleyType.FOLEY_CRAZYDAVELONG);
			mSoundSystem.StopFoley(FoleyType.FOLEY_CRAZYDAVEEXTRALONG);
			mSoundSystem.StopFoley(FoleyType.FOLEY_CRAZYDAVECRAZY);
		}

		public bool UpdatePlayerProfileForFinishingLevel()
		{
			bool flag = false;
			if (IsAdventureMode())
			{
				int level = mBoard.mLevel;
				if (level == 50)
				{
					if (mPlayerInfo.mIZombieUnlocked == 3 && HasBeatenChallenge(GameMode.GAMEMODE_PUZZLE_I_ZOMBIE_3))
					{
						mPlayerInfo.mIZombieUnlocked++;
					}
					if (mPlayerInfo.mVasebreakerUnlocked == 3 && HasBeatenChallenge(GameMode.GAMEMODE_SCARY_POTTER_3))
					{
						mPlayerInfo.mVasebreakerUnlocked++;
					}
					mPlayerInfo.SetLevel(1);
					mPlayerInfo.mFinishedAdventure++;
					if (mPlayerInfo.mFinishedAdventure == 1)
					{
						mPlayerInfo.mNeedsMessageOnGameSelector = true;
						mPlayerInfo.mMiniGamesUnlockable = 19;
						int num = 0;
						if (HasBeatenChallenge(GameMode.GAMEMODE_CHALLENGE_WAR_AND_PEAS))
						{
							num++;
						}
						if (HasBeatenChallenge(GameMode.GAMEMODE_CHALLENGE_WALLNUT_BOWLING))
						{
							num++;
						}
						if (HasBeatenChallenge(GameMode.GAMEMODE_CHALLENGE_SLOT_MACHINE))
						{
							num++;
						}
						mPlayerInfo.mMiniGamesUnlocked += num;
					}
				}
				else
				{
					mPlayerInfo.SetLevel(level + 1);
				}
				if (!HasFinishedAdventure() && level == 34)
				{
					mPlayerInfo.mNeedsMagicTacoReward = true;
				}
				if (!HasFinishedAdventure() && level == 44)
				{
					mPlayerInfo.mNeedsMagicBaconReward = true;
				}
				if ((level >= 22 || mPlayerInfo.mFinishedAdventure > 0) && !mPlayerInfo.mHasUnlockedMinigames)
				{
					mPlayerInfo.UnlockFirstMiniGames();
				}
				if ((level >= 36 || mPlayerInfo.mFinishedAdventure > 0) && !mPlayerInfo.mHasUnlockedPuzzleMode)
				{
					mPlayerInfo.UnlockPuzzleMode();
				}
			}
			else if (!IsQuickPlayMode())
			{
				if (IsSurvivalMode())
				{
					if (mBoard.IsFinalSurvivalStage())
					{
						flag = !HasBeatenChallenge(mGameMode);
						mBoard.SurvivalSaveScore();
						if (flag && HasFinishedAdventure())
						{
							int numTrophies = GetNumTrophies(ChallengePage.CHALLENGE_PAGE_SURVIVAL);
							if (numTrophies != 8 && numTrophies != 9)
							{
								mPlayerInfo.mHasNewSurvival = true;
							}
						}
					}
				}
				else if (IsPuzzleMode())
				{
					flag = !HasBeatenChallenge(mGameMode);
					int currentChallengeIndex = GetCurrentChallengeIndex();
					mPlayerInfo.mChallengeRecords[currentChallengeIndex]++;
					if (!HasFinishedAdventure() && (mGameMode == GameMode.GAMEMODE_SCARY_POTTER_3 || mGameMode == GameMode.GAMEMODE_PUZZLE_I_ZOMBIE_3))
					{
						flag = false;
					}
					if (flag)
					{
						if (IsScaryPotterLevel())
						{
							mPlayerInfo.mHasNewVasebreaker = true;
							mPlayerInfo.mVasebreakerUnlocked++;
							if (mPlayerInfo.mFinishedAdventure == 0 && mPlayerInfo.mVasebreakerUnlocked > 3)
							{
								mPlayerInfo.mVasebreakerUnlocked = 3;
							}
							if (mPlayerInfo.mVasebreakerUnlocked > 10)
							{
								mPlayerInfo.mVasebreakerUnlocked = 10;
							}
						}
						else
						{
							mPlayerInfo.mHasNewIZombie = true;
							mPlayerInfo.mIZombieUnlocked++;
							if (mPlayerInfo.mFinishedAdventure == 0 && mPlayerInfo.mIZombieUnlocked > 3)
							{
								mPlayerInfo.mIZombieUnlocked = 3;
							}
							if (mPlayerInfo.mIZombieUnlocked > 10)
							{
								mPlayerInfo.mIZombieUnlocked = 10;
							}
						}
					}
				}
				else
				{
					flag = !HasBeatenChallenge(mGameMode);
					int currentChallengeIndex2 = GetCurrentChallengeIndex();
					mPlayerInfo.mChallengeRecords[currentChallengeIndex2]++;
					if (mPlayerInfo.mMiniGamesUnlocked < mPlayerInfo.mMiniGamesUnlockable)
					{
						mPlayerInfo.mMiniGamesUnlocked++;
					}
					if (flag && HasFinishedAdventure())
					{
						int numTrophies2 = GetNumTrophies(ChallengePage.CHALLENGE_PAGE_CHALLENGE);
						if (numTrophies2 <= 17)
						{
							mPlayerInfo.mHasNewMiniGame = true;
						}
					}
				}
			}
			int numTrophies3 = GetNumTrophies(ChallengePage.CHALLENGE_PAGE_CHALLENGE);
			if (numTrophies3 == 19)
			{
				ReportAchievement.GiveAchievement(AchievementId.BeyondTheGrave);
			}
			WriteCurrentUserConfig();
			return flag;
		}

		public bool CanDoDaisyMode()
		{
			return false;
		}

		public bool CanDoPinataMode()
		{
			return false;
		}

		public bool CanDoDanceMode()
		{
			return false;
		}

		public override void SetSfxVolume(double theVolume)
		{
			base.SetSfxVolume(theVolume);
			mPlayerInfo.mSoundVolume = theVolume;
		}

		public override void SetMusicVolume(double theVolume)
		{
			base.SetMusicVolume(theVolume);
			if (mPlayerInfo != null)
			{
				mPlayerInfo.mMusicVolume = theVolume;
			}
		}

		public bool SaveFileExists()
		{
			string savedGameName = LawnCommon.GetSavedGameName(GameMode.GAMEMODE_ADVENTURE, (int)mPlayerInfo.mId);
			return base.FileExists(savedGameName);
		}

		public void Vibrate()
		{
			if (mPlayerInfo == null || !mPlayerInfo.mDoVibration)
			{
				return;
			}
			base.DoVibration();
		}

		public override void MoviePlayerContentPreloadDidFinish(bool succeeded)
		{
			if (mCreditScreen != null)
			{
				mCreditScreen.VideoLoaded(succeeded);
			}
		}

		public override void MoviePlayerPlaybackDidFinish()
		{
			if (mCreditScreen != null)
			{
				mCreditScreen.VideoFinished();
			}
		}

		public int GetAchievementIcon(AchievementId theAchievement)
		{
			return GameConstants.AchievementInfo[(int)theAchievement].mImageId;
		}

		public string GetAchievementName(AchievementId theAchievement)
		{
			return TodStringFile.TodStringTranslate(GameConstants.AchievementInfo[(int)theAchievement].mName);
		}

		public string GetAchievementDescription(AchievementId theAchievement)
		{
			return TodStringFile.TodStringTranslate(GameConstants.AchievementInfo[(int)theAchievement].mDesc);
		}

		public override bool ShouldAutorotateToInterfaceOrientation(UI_ORIENTATION theOrientation)
		{
			return base.ShouldAutorotateToInterfaceOrientation(theOrientation) && (theOrientation == UI_ORIENTATION.UI_ORIENTATION_LANDSCAPE_LEFT || theOrientation == UI_ORIENTATION.UI_ORIENTATION_LANDSCAPE_RIGHT);
		}

		private const string PLACEHOLDER_PLAYER = "{PLAYER_NAME}";

		private const string PLACEHOLDER_MONEY = "{MONEY}";

		private const string PLACEHOLDER_UPGRADECOST = "{UPGRADE_COST}";

		private const string PLACEHOLDER_CRAZYDAVE_0 = "[CRAZY_DAVE_{0}]";

		public static string AppVersionNumber = "1.4";

		public Board mBoard;

		public TitleScreen mTitleScreen;

		public GameSelector mGameSelector;

		public SeedChooserScreen mSeedChooserScreen;

		public AwardScreen mAwardScreen;

		public CreditScreen mCreditScreen;

		public TodFoley mSoundSystem;

		public LinkedList<ButtonWidget> mControlButtonList = new LinkedList<ButtonWidget>();

		public LinkedList<Image> mCreatedImageList = new LinkedList<Image>();

		public string mReferId;

		public string mRegisterLink;

		public string mMod;

		public bool mRegisterResourcesLoaded;

		public bool mTodCheatKeys;

		public GameMode mGameMode;

		public GameScenes mGameScene;

		public bool mLoadingZombiesThreadCompleted;

		public bool mFirstTimeGameSelector;

		public int mGamesPlayed;

		public int mMaxExecutions;

		public int mMaxPlays;

		public int mMaxTime;

		public bool mEasyPlantingCheat;

		public ZenGarden mZenGarden;

		public EffectSystem mEffectSystem;

		public ReanimatorCache mReanimatorCache;

		public ProfileMgr mProfileMgr;

		private PlayerInfo _playerInfo;

		public LevelStats mLastLevelStats;

		public bool mCloseRequest;

		public int mAppCounter;

		public Music mMusic;

		public Reanimation mCrazyDaveReanimID;

		public CrazyDaveState mCrazyDaveState;

		public int mCrazyDaveBlinkCounter;

		public Reanimation mCrazyDaveBlinkReanimID;

		public int mCrazyDaveMessageIndex;

		public string mCrazyDaveMessageText = string.Empty;

		public int mAppRandSeed;

		public int mSessionID;

		public int mPlayTimeActiveSession;

		public int mPlayTimeInactiveSession;

		public BoardResult mBoardResult;

		public bool mKilledYetiAndRestarted;

		public TypingCheck mKonamiCheck;

		public TypingCheck mMustacheCheck;

		public TypingCheck mMoustacheCheck;

		public TypingCheck mSuperMowerCheck;

		public TypingCheck mSuperMowerCheck2;

		public TypingCheck mFutureCheck;

		public TypingCheck mPinataCheck;

		public TypingCheck mDanceCheck;

		public TypingCheck mDaisyCheck;

		public TypingCheck mSukhbirCheck;

		public bool mMustacheMode;

		public bool mSuperMowerMode;

		public bool mFutureMode;

		public bool mPinataMode;

		public bool mDanceMode;

		public bool mDaisyMode;

		public bool mSukhbirMode;

		public TrialType mTrialType;

		public bool mDebugTrialLocked;

		public bool mMuteSoundsForCutscene;

		private string mLastBackgroundResGroupLoaded;

		private string mLastZombieNoteResGroupLoaded;

		private string mLastStoreResGroupLoaded;

		private string mLastPaperGroupLoaded;

		private string mLastZenGardenResourceLoaded;

		private string mLastUpsellResGroupLoaded;

		public GameMode mRestoreGameMode;

		public RestoreLocation mRestoreLocation;

		public bool checkGiveAchievements;

		public AchievementId achievementToCheck = AchievementId.MAX_ACHIEVEMENTS;

		private Dictionary<int, string> cachedStageStrings = new Dictionary<int, string>();

		public ChallengeScreen mChallengeScreen;

		public LeaderboardScreen mLeaderboardScreen;

		private static Dictionary<string, string> noClickStringCache = new Dictionary<string, string>(10);

		public LawnMessageBoxListener mLawnMessageBoxListener;

		public Widget mOldFocus;

		private static Dictionary<int, string> moneyStrings = new Dictionary<int, string>();

		private static Dictionary<int, string> cachedIntToString = new Dictionary<int, string>();

		private bool leaderboardLoaded;

		private bool gamePlayLoaded;

		private bool cachedLoaded;

		private bool zenGardenLoaded;

		private bool mainMenuLoaded;

		private bool pileLoaded;

		private class TableTmp
		{
			public TableTmp(int aNormal, int aAdditive)
			{
				mNormalImageId = aNormal;
				mAdditiveImageId = aAdditive;
			}

			public int mNormalImageId;

			public int mAdditiveImageId;
		}
	}
}
