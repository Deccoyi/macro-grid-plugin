---
layout: page
sidebar: false
title: Eklenti Mağazası
---

<script setup>
import { data } from '../../.vitepress/store.data'
</script>

<PluginStore :plugins="data" />
