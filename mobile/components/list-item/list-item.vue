<template>
  <view class="list-item" @click="$emit('click')">
    <view v-if="avatar || avatarUrl" class="list-item__avatar">
      <u-avatar :src="avatarUrl" :text="avatarText" size="40" />
    </view>
    <view class="list-item__body">
      <view class="list-item__title">{{ title }}</view>
      <view v-if="subtitle" class="list-item__subtitle">{{ subtitle }}</view>
      <slot name="extra" />
    </view>
    <view v-if="showArrow" class="list-item__arrow">
      <u-icon name="arrow-right" color="#bfbfbf" size="16" />
    </view>
  </view>
</template>

<script setup>
defineProps({
  title: { type: String, default: '' },
  subtitle: { type: String, default: '' },
  avatarUrl: { type: String, default: '' },
  avatarText: { type: String, default: '' },
  avatar: { type: Boolean, default: false },
  showArrow: { type: Boolean, default: true }
});

defineEmits(['click']);
</script>

<style scoped lang="scss">
.list-item {
  display: flex;
  align-items: center;
  padding: 24rpx 0;
  border-bottom: 1rpx solid $uni-border-color;

  &__avatar {
    margin-right: 20rpx;
  }

  &__body {
    flex: 1;
    min-width: 0;
  }

  &__title {
    font-size: 30rpx;
    color: $uni-text-color;
    font-weight: 500;
  }

  &__subtitle {
    margin-top: 8rpx;
    font-size: 24rpx;
    color: $uni-text-color-secondary;
  }

  &__arrow {
    margin-left: 12rpx;
  }
}
</style>
