# Proposal: widen the site workflow path filters

`pages.yml` only watches `OBS/src/ObsPlugin.cs` from the OBS plugin because the docs import just that file. If the docs
start importing more OBS sources (for example one action from `OBS/src/Actions/`), add the folder to both path lists.
No change made now.
