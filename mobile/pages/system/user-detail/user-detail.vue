<template>
  <view class="page page-container">
    <u-loading-icon v-if="loading" text="加载中..." />

    <template v-else-if="user">
      <view class="card profile-card">
        <view class="flex-row">
          <u-avatar :src="avatarUrl" :text="avatarText" size="64" bg-color="#1677ff" />
          <view class="profile-card__info flex-1">
            <text class="profile-card__name">{{ user.displayName || user.userName }}</text>
            <text class="profile-card__meta">@{{ user.userName }}</text>
          </view>
          <u-tag :text="user.isActive ? '启用' : '禁用'" :type="user.isActive ? 'success' : 'info'" />
        </view>
      </view>

      <view class="card card--flat">
        <text class="section-title">基本信息</text>
        <view class="info-row flex-row">
          <text class="muted">邮箱</text>
          <text class="flex-1 info-row__value text-ellipsis">{{ user.email || '-' }}</text>
        </view>
        <view class="info-row flex-row info-row--border">
          <text class="muted">创建时间</text>
          <text class="info-row__value">{{ formatDateTime(user.createdAt) }}</text>
        </view>
        <view class="info-row flex-row">
          <text class="muted">用户 ID</text>
          <text class="info-row__value">{{ user.id }}</text>
        </view>
      </view>

      <view class="card card--flat">
        <text class="section-title">角色</text>
        <view class="tag-wrap">
          <u-tag v-for="role in user.roles" :key="role" :text="role" type="primary" plain />
          <text v-if="!user.roles?.length" class="muted">未分配角色</text>
        </view>
      </view>

      <view class="card card--flat">
        <text class="section-title">权限概览</text>
        <view class="tag-wrap">
          <u-tag
            v-for="perm in previewPermissions"
            :key="perm"
            :text="perm"
            type="info"
            plain
            size="mini" />
        </view>
        <text v-if="user.permissions?.length > previewPermissions.length" class="muted more-tip">
          共 {{ user.permissions.length }} 项权限
        </text>
      </view>

      <view v-if="canEdit" class="actions">
        <u-button type="primary" text="编辑用户" @click="goEdit" />
      </view>
    </template>

    <empty-view v-else text="用户不存在" />
  </view>
</template>

<script setup>
import { computed, ref } from 'vue';
import { onLoad } from '@dcloudio/uni-app';
import EmptyView from '@/components/empty-view/empty-view.vue';
import * as userService from '@/service/user.service';
import { useUserStore } from '@/store/user';
import { formatDateTime, resolveAssetUrl } from '@/utils/format';

const userStore = useUserStore();
const loading = ref(false);
const user = ref(null);
const userId = ref(null);

const avatarUrl = computed(() => resolveAssetUrl(user.value?.avatarUrl));
const avatarText = computed(() => (user.value?.userName || 'U').slice(0, 1).toUpperCase());
const previewPermissions = computed(() => (user.value?.permissions || []).slice(0, 12));
const canEdit = computed(() => userStore.hasPermission('system.users:edit'));

onLoad(options => {
  userId.value = Number(options.id);
  if (!userStore.isLoggedIn) {
    uni.reLaunch({ url: '/pages/login/login' });
    return;
  }
  if (!userStore.hasPermission('system.users:view')) {
    uni.showToast({ title: '无权查看用户', icon: 'none' });
    return;
  }
  loadUser();
});

async function loadUser() {
  loading.value = true;
  try {
    user.value = await userService.getUserById(userId.value);
  } catch (err) {
    uni.showToast({ title: err.message || '加载失败', icon: 'none' });
    user.value = null;
  } finally {
    loading.value = false;
  }
}

function goEdit() {
  uni.navigateTo({ url: `/pages/system/user-form/user-form?id=${userId.value}` });
}
</script>

<style scoped lang="scss">
.actions {
  margin-top: 8rpx;
}
.profile-card {
  &__info {
    margin-left: 24rpx;
  }

  &__name {
    display: block;
    font-size: 36rpx;
    font-weight: 600;
    color: $uni-text-color;
  }

  &__meta {
    display: block;
    margin-top: 8rpx;
    font-size: 24rpx;
    color: $uni-text-color-secondary;
  }
}

.info-row {
  padding: 22rpx 0;
  font-size: 28rpx;

  &--border {
    border-top: 1rpx solid $uni-border-color;
    border-bottom: 1rpx solid $uni-border-color;
  }

  &__value {
    text-align: right;
    margin-left: 24rpx;
  }
}

.tag-wrap {
  display: flex;
  flex-direction: row;
  flex-wrap: wrap;
  gap: 12rpx;
}

.more-tip {
  display: block;
  margin-top: 16rpx;
}
</style>
