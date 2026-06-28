<template>
  <view class="adaptive-card" :class="{ 'adaptive-card--clickable': clickable }" @click="onClick">
    <view v-if="title || $slots.header" class="adaptive-card__header">
      <slot name="header">
        <text class="adaptive-card__title">{{ title }}</text>
        <text v-if="extra" class="adaptive-card__extra">{{ extra }}</text>
      </slot>
    </view>
    <view class="adaptive-card__body">
      <slot />
    </view>
  </view>
</template>

<script setup>
const props = defineProps({
  title: { type: String, default: '' },
  extra: { type: String, default: '' },
  clickable: { type: Boolean, default: false }
});

const emit = defineEmits(['click']);

function onClick() {
  if (props.clickable) emit('click');
}
</script>

<style scoped lang="scss">
.adaptive-card {
  background: #fff;
  border-radius: $card-radius;
  padding: $page-padding;
  margin-bottom: 24rpx;
  box-shadow: 0 4rpx 16rpx rgba(0, 0, 0, 0.04);

  &--clickable:active {
    opacity: 0.92;
  }

  &__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 16rpx;
  }

  &__title {
    font-size: 32rpx;
    font-weight: 600;
    color: $uni-text-color;
  }

  &__extra {
    font-size: 24rpx;
    color: $uni-text-color-secondary;
  }
}
</style>
