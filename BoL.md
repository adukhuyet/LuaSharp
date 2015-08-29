# Help me
These are functions that I have NO idea what they do and what they return

If I am missing functions, please submit an issue.

| Function                         | Type |
|----------------------------------|:----:|
|                                  |      |
|                                  |      |
|                                  |      |
|                                  |      |

# CallBacks
| Function                         |  Status  | Working    |
| -------------------------------- |:--------:|:----------:|
| OnLoad()                         |Done      | Unknown    |
| OnDraw()                         |Done      | Unknown    |
| OnUnload()                       |**TODO**  | Unfinished |
| OnCreateObj(object)              |Done      | Unknown    |
| OnDeleteObj(object)              |Done      | Unknown    |
| OnWndMsg(msg,wParam)             |Done      | Unknown    |
| OnProcessSpell(object,spellProc) |Done      | Unknown    |
| OnSendChat(text)                 |Done      | Unknown    |
| OnReset()                        |**TODO**  | Unfinished |

# Globals
## Functions
| Function                                            |  Status  | Working    |
| --------------------------------------------------- |:--------:|:----------:|
| EnableZoomHack()                                    |          | Unfinished |
| IsKeyPressed(wParam)                                |          | Unfinished |
| IsKeyDown(wParam)                                   |          | Unfinished |
| CastSpell(iSpell)                                   |Done      |Unknown     |
| CastSpell(iSpell,x,z)                               |Done      |Unknown     |
| CastSpell(iSpell,target)                            |Done      |Unknown     |
| LevelSpell(iSpell)                                  |Done      |Unknown     |
| PrintChat(text)                                     |Done      |Unknown     |
| SendChat(text)                                      |Done      |Unknown     |
| BlockChat()                                         |          | Unfinished |
| DrawText(text,size,x,y,ARGB)                        |Done      |Unknown     |
| DrawLine(x1, y1, x2, y2, size, ARGB)                |Done      |Unknown     |
| DrawRectangle(x, y, width, height, ARGB)            |Done      |Unknown     |
| DrawCircle(x,y,z,size,RGB)                          |Done      |Unknown     |
| PrintFloatText(target,iMode,text)                   |          | Unfinished |
| PingSignal(iMode,x,y,z,bPing)                       |Done      |Unknown     |
| GetMyHero()                                         |Done      |Unknown     |
| GetTarget()                                         |Done      |Unknown     |
| GetTickCount()                                      |Done      |Unknown     |
| GetLatency()                                        |Done      |Unknown     |
| GetCursorPos()                                      |Done      |Unknown     |
| WorldToScreen(unit)                                 |Done      |Unknown     |
| SetTarget(unit)                                     |Done      |Unknown     |
| createSprite(szFile)                                |          | Unfinished |
| CLoLPacket(header)                                  |          | Unfinished |
| SendPacket(LoLPacket)                               |          | Unfinished |
| BuyItem(itemID)                                     |Done      |Unknown     |
| SellItem(iSlot)                                     |Done      |Unknown     |
| IsItemPurchasable(itemID)                           |          | Unfinished |
| IsRecipePurchasable(recipeID)                       |          | Unfinished |
| DrawArrow(pos,arrowDir,fIdk1,fWidth,fIdk2,dwColor)  |          | Unfinished |
| IsWallOfGrass(pos)                                  |Done      |Unknown     |
| UpdateWindow()                                      |          | Unfinished |
| GetKey(hotkey)                                      |          | Unfinished |
| RGBA(r,g,b,a)                                       |          | Unfinished |
| RGB(r,g,b )                                         |          | Unfinished |
| KillProcess(procName)                               |Done      |Unknown     |
| GetGameTimer()                                      |Done      |Unknown     |

<!--|                                                     |          |            |-->
##Members
| Member Name |  State  |
|:-----------:|:-------:|
| objManager  | Unknown |
| heroManager | Unknown |
| mousePos    | Unknown |
| cameraPos   | Unknown |

## Custom Functions
| Function                                                 | What it does                |
| DrawPoint(float x, float y, float thickness, ARGB color) | Draws a point on the screen |
| DrawPoint(float x, float y, float thickness, uint color) | Draws a point on the screen |

### objManager Instance
#### Members
| Instance Member Name |  State  |
|:--------------------:|:-------:|
|objManager.maxObjects | Unknown |
|objManager.iCount     | Unknown |

#### Methods
| Methods Member Name         |  State  | Returns     |
|:---------------------------:|:-------:|:-----------:|
|objManager:getObject(iIndex) | Unknown |unit (object)|

### heroManager Instance
