<template>
  <view class="fab" v-if="visible" @click="$emit('click')">
    <u-icon name="plus" color="#fff" size="22" />
  </view>
</template>

<script setup>
defineProps({
  visible: { type: Boolean, default: true }
});

defineEmits(['click']);
</script>

<style scoped lang="scss">
.fab {
  position: fixed;
  right: 32rpx;
  bottom: calc(140rpx + env(safe-area-inset-bottom));
  width: 96rpx;
  height: 96rpx;
  border-radius: 50%;
  background: linear-gradient(135deg, #1677ff, #0958d9);
  box-shadow: 0 12rpx 32rpx rgba(22, 119, 255, 0.35);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 99;
}
</style>
