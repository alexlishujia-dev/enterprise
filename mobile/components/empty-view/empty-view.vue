<template>
  <view class="empty-view">
    <u-empty :text="text" :mode="mode" />
    <slot />
  </view>
</template>

<script setup>
defineProps({
  text: { type: String, default: '暂无数据' },
  mode: { type: String, default: 'data' }
});
</script>

<style scoped lang="scss">
.empty-view {
  padding: 80rpx 0;
}
</style>
