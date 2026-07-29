Hey! Shop's basically done — daily rotation, prices from JSON, BUY? popup, back/close signs all working.

Just need a couple of things from you:

1. **The new shop background.** The one we have (Fishingshop2) is 768x384, which is 2:1 — the game
   is 16:9, so we lose about 15% off the right side, and that's where the shelves are. It's also
   only got the two sign rows baked in; your mockup has a third row of pegs under "sal-T shop" for
   the back/close signs, plus more shelf space. Could you send it at 1920x1080, same 6 frames,
   same file names?

2. **The hat icons are framed inconsistently.** The FisherMan_Hat_* ones are 64x64 files with the
   actual hat only filling 20-38% of it, while cap/beret/hat/hat2/paper_boat are cropped tight.
   Makes them render at wildly different sizes. I've hacked around it in code, but tightly cropped
   (or consistently padded) versions would be better.

3. Quick one — the skull tab still says "W.I.P" under it. Want me to drop that and use the new
   skull as the icon?

Also, prices now live in shop_config.json so you can tweak them yourself, no rebuild needed.
Currently 200 / 500 / 1000 — shout if you want different numbers.

Thanks!
